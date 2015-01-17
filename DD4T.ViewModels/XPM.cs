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

namespace DD4T.ViewModels.XPM
{
    public static class XPM
    {
        #region public extension methods
        public static MvcHtmlString XpmEditableField<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IDD4TViewModel
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            return SiteEditableField<TModel, TProp>(model, fields, fieldProp, index);
        }
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
        public static MvcHtmlString StartXpmEditingZone(this IComponentPresentationViewModel model, string region = null)
        {
            return new MvcHtmlString(SiteEditService.GenerateSiteEditComponentTag(model.ComponentPresentation, region));
        }
        #endregion

        #region private methods
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
        public static IField FieldFor<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IDD4TViewModel
        {
            var fieldProp = GetFieldProperty(propertyLambda);
            var fields = fieldProp.FieldAttribute.IsMetadata ? model.MetadataFields : model.Fields;
            var fieldName = fieldProp.FieldAttribute.FieldName;
            var field = fields.ContainsKey(fieldName) ? fields[fieldName] : null;
            return field;
        }


    }
}