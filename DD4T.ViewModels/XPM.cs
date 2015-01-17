﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using DD4T.Mvc.SiteEdit;
using DD4T.ViewModels.Reflection;
using DD4T.ViewModels.Attributes;
using DD4T.ViewModels.Contracts;
using System.Reflection;
using DD4T.ContentModel;
using System.Collections;

namespace DD4T.ViewModels.XPM
{
    //TODO: Refactor and cut down code bloat in this class
    public static class XPM
    {
        #region public extension methods
        /// <summary>
        /// Renders both XPM Markup and Field Value 
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup and field value</returns>
        public static MvcHtmlString XpmEditableField<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IDD4TViewModel
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            return SiteEditableField<TModel, TProp>(model, fields, fieldProp, index);
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
        ///     @model.XpmEditableField(m => m.Content, content);
        /// }
        /// </example>
        /// <returns>XPM Markup and field value</returns>
        public static MvcHtmlString XpmEditableField<TModel, TProp, TItem>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, TItem item) 
            where TModel : IDD4TViewModel
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            int index = IndexOf(fieldProp, model, item);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            return SiteEditableField<TModel, TProp>(model, fields, fieldProp, index);
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
        public static MvcHtmlString XpmMarkupFor<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IDD4TViewModel
        {
            bool siteEditEnabled = true;
            if (model is IComponentPresentationViewModel)
                siteEditEnabled = SiteEditService.IsSiteEditEnabled(((IComponentPresentationViewModel)model).ComponentPresentation.Component);
            if (siteEditEnabled)
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
        public static MvcHtmlString XpmMarkupFor<TModel, TProp, TItem>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, TItem item) 
            where TModel : IDD4TViewModel
        {
            bool siteEditEnabled = true;
            if (model is IComponentPresentationViewModel)
                siteEditEnabled = SiteEditService.IsSiteEditEnabled(((IComponentPresentationViewModel)model).ComponentPresentation.Component);
            if (siteEditEnabled)
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
        public static MvcHtmlString StartXpmEditingZone(this IComponentPresentationViewModel model, string region = null)
        {
            return new MvcHtmlString(SiteEditService.GenerateSiteEditComponentTag(model.ComponentPresentation, region));
        }
        #endregion

        #region private methods
        private static int IndexOf(this IEnumerable enumerable, object obj)
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
        private static int IndexOf<T>(FieldAttributeProperty fieldProp, object model, T item)
        {
            int index = -1;
            object value = fieldProp.Get(model);
            if (value is IEnumerable<T>)
            {
                IEnumerable<T> list = (IEnumerable<T>)value;
                index = list.IndexOf(item);
            }
            else throw new FormatException(String.Format("Generic type of property type {0} does not match generic type of item {1}", value.GetType().Name, typeof(T).Name));
            return index;
        }
        private static FieldAttributeProperty GetFieldProperty<TModel, TProp>(Expression<Func<TModel, TProp>> propertyLambda)
        {
            PropertyInfo property = ReflectionCache.GetPropertyInfo(propertyLambda);
            return GetFieldProperty(typeof(TModel), property);
        }
        private static MvcHtmlString SiteEditableField<TModel, TProp>(object model, IFieldSet fields, FieldAttributeProperty fieldProp, int index)
        {
            string markup = string.Empty;
            object value = null;
            string propValue = string.Empty;
            try
            {
                var field = GetField(fields, fieldProp);
                markup = GenerateSiteEditTag(field, index);
                value = fieldProp.Get(model);
                propValue = value == null ? string.Empty : value.ToString();
            }
            catch (NullReferenceException)
            {
                return null;
            }
            return new MvcHtmlString(markup + propValue);
        }

        private static string GenerateSiteEditTag(IField field, int index)
        {
            var result = index > 0 ? SiteEditService.GenerateSiteEditFieldTag(field, index)
                            : SiteEditService.GenerateSiteEditFieldTag(field);
            return result ?? string.Empty;
        }
        private static IField GetField(IFieldSet fields, FieldAttributeProperty fieldProp)
        {

            var fieldName = fieldProp.FieldAttribute.FieldName;
            return fields.ContainsKey(fieldName) ? fields[fieldName] : null;
        }

        private static FieldAttributeProperty GetFieldProperty(Type type, PropertyInfo property)
        {
            var props = ReflectionCache.GetFieldProperties(type);
            return props.FirstOrDefault(x => x.Name == property.Name);
        }

        private static MvcHtmlString XpmMarkupFor(IFieldSet fields, FieldAttributeProperty fieldProp, int index)
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
        //for testing only
        [Obsolete]
        public static IField FieldFor<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IDD4TViewModel
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            var fieldName = fieldProp.FieldAttribute.FieldName;
            var field = fields.ContainsKey(fieldName) ? fields[fieldName] : null;
            return field;
        }
        [Obsolete]
        public static IField FieldFor<TModel, TProp, TItem>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, TItem item) where TModel : IDD4TViewModel
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            int index = IndexOf(fieldProp, model, item);
            var fieldName = fieldProp.FieldAttribute.FieldName;
            var field = fields.ContainsKey(fieldName) ? fields[fieldName] : null;
            return field;
        }

    }
}
