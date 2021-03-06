﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ViewModels.Contracts;
using System.Web.Mvc;
using System.Linq.Expressions;
using System.Collections;
using DD4T.ViewModels.Reflection;
using System.Reflection;
using DD4T.ContentModel;

namespace DD4T.ViewModels.XPM
{
    /// <summary>
    /// Renders XPM Markup for View Models
    /// </summary>
    /// <typeparam name="TModel">Type of the View Model</typeparam>
    public class XpmRenderer<TModel> : IXpmRenderer<TModel> where TModel : IDD4TViewModel
    {
        //This is just an OO implementation of the static extension methods... which one is better
        private IDD4TViewModel model;
        private IXpmMarkupService xpmMarkupService = new XpmMarkupService();
        public XpmRenderer(IDD4TViewModel model)
        {
            this.model = model;
        }
        /// <summary>
        /// Gets or sets the XPM Markup Service used to render the XPM Markup for the XPM extension methods
        /// </summary>
        public IXpmMarkupService XpmMarkupService
        {
            get { return xpmMarkupService; }
            set { xpmMarkupService = value; }
        }
        #region XPM Renderer methods
        /// <summary>
        /// Renders both XPM Markup and Field Value 
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup and field value</returns>
        public MvcHtmlString XpmEditableField<TProp>(Expression<Func<TModel, TProp>> propertyLambda, int index = -1)
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            return SiteEditable<TProp>(model, fields, fieldProp, index);
        }
        /// <summary>
        /// Renders both XPM Markup and Field Value for a multi-value field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="item">The particular value of the multi-value field</param>
        /// <example>
        /// foreach (var content in model.Content)
        /// {
        ///     @model.XpmEditableFieldField(m => m.Content, content);
        /// }
        /// </example>
        /// <returns>XPM Markup and field value</returns>
        public MvcHtmlString XpmEditableField<TProp, TItem>(Expression<Func<TModel, TProp>> propertyLambda, TItem item)
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            int index = IndexOf(fieldProp, model, item);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            return SiteEditable<TProp>(model, fields, fieldProp, index);
        }
        /// <summary>
        /// Renders the XPM markup for a field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup</returns>
        public MvcHtmlString XpmMarkupFor<TProp>(Expression<Func<TModel, TProp>> propertyLambda, int index = -1)
        {
            if (IsSiteEditEnabled(model))
            {
                var fieldProp = GetFieldProperty(propertyLambda);
                var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
                return XpmMarkupFor(fields, fieldProp, index);
            }
            else return null;
        }
        /// <summary>
        /// Renders XPM Markup for a multi-value field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="item">The particular value of the multi-value field</param>
        /// <example>
        /// foreach (var content in model.Content)
        /// {
        ///     @model.XpmMarkupFor(m => m.Content, content);
        ///     @content;
        /// }
        /// </example>
        /// <returns>XPM Markup</returns>
        public MvcHtmlString XpmMarkupFor<TProp, TItem>(Expression<Func<TModel, TProp>> propertyLambda, TItem item)
        {
            if (IsSiteEditEnabled(model))
            {
                var fieldProp = GetFieldProperty(propertyLambda);
                int index = IndexOf(fieldProp, model, item);
                var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
                return XpmMarkupFor(fields, fieldProp, index);
            }
            else return null;
        }
        /// <summary>
        /// Renders the XPM Markup for a Component Presentation
        /// </summary>
        /// <param name="model">Model</param>
        /// <param name="region">Region</param>
        /// <returns>XPM Markup</returns>
        public MvcHtmlString StartXpmEditingZone(string region = null)
        {
            if (model is IComponentPresentationViewModel)
                return new MvcHtmlString(XpmMarkupService.RenderXpmMarkupForComponent(((IComponentPresentationViewModel)model).ComponentPresentation, region));
            else return null;
        }
        #endregion

        #region private methods
        private bool IsSiteEditEnabled(IDD4TViewModel model)
        {
            return XpmMarkupService.IsSiteEditEnabled(model.PublicationId);
        }

        private int IndexOf(IEnumerable enumerable, object obj)
        {
            if (obj != null)
            {
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (item.Equals(obj)) return i;
                    i++;
                }
            }
            return -1;
        }
        private int IndexOf<T>(FieldAttributeProperty fieldProp, object model, T item)
        {
            int index = -1;
            object value = fieldProp.Get(model);
            if (value is IEnumerable<T>)
            {
                IEnumerable<T> list = (IEnumerable<T>)value;
                index = IndexOf(list, item);
            }
            else throw new FormatException(String.Format("Generic type of property type {0} does not match generic type of item {1}", value.GetType().Name, typeof(T).Name));
            return index;
        }
        private FieldAttributeProperty GetFieldProperty<TProp>(Expression<Func<TModel, TProp>> propertyLambda)
        {
            PropertyInfo property = ReflectionCache.GetPropertyInfo(propertyLambda);
            return GetFieldProperty(typeof(TModel), property);
        }
        private MvcHtmlString SiteEditable<TProp>(IDD4TViewModel model, IFieldSet fields, FieldAttributeProperty fieldProp, int index)
        {
            string markup = string.Empty;
            object value = null;
            string propValue = string.Empty;
            try
            {
                var field = GetField(fields, fieldProp);
                markup = IsSiteEditEnabled(model) ? GenerateSiteEditTag(field, index) : string.Empty;
                value = fieldProp.Get(model);
                propValue = value == null ? string.Empty : value.ToString();
            }
            catch (NullReferenceException)
            {
                return null;
            }
            return new MvcHtmlString(markup + propValue);
        }

        private string GenerateSiteEditTag(IField field, int index)
        {
            return XpmMarkupService.RenderXpmMarkupForField(field, index);
        }
        private IField GetField(IFieldSet fields, FieldAttributeProperty fieldProp)
        {
            var fieldName = fieldProp.FieldAttribute.FieldName;
            return fields.ContainsKey(fieldName) ? fields[fieldName] : null;
        }

        private FieldAttributeProperty GetFieldProperty(Type type, PropertyInfo property)
        {
            var props = ReflectionCache.GetFieldProperties(type);
            return props.FirstOrDefault(x => x.Name == property.Name);
        }

        private MvcHtmlString XpmMarkupFor(IFieldSet fields, FieldAttributeProperty fieldProp, int index)
        {
            try
            {
                return new MvcHtmlString(GenerateSiteEditTag(GetField(fields, fieldProp), index));
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
        #endregion
    }
}
