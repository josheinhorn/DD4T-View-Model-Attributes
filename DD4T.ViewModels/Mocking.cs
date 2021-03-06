﻿using DD4T.ContentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ViewModels.Contracts;
using DD4T.ViewModels.Attributes;
using DD4T.ViewModels.Reflection;
using System.Reflection;
using System.Web;

namespace DD4T.ViewModels
{
    internal static class TridionHelper
    {
        public static string GetRandomTcmUri(int publicationId, int itemType = 0)
        {
            string suffix = null;
            if (itemType > 0)
                suffix = "-" + itemType;
            return string.Format("tcm:{0}-{1}{2}", publicationId, new Random().Next(1, 1000), suffix);
        }
    }
    public class CTMocker : ICTMocker
    {
        protected string viewModelKeyField;
        private int publicationId = 1;
        public CTMocker(int publicationId, string viewModelKeyField = null)
        {
            this.publicationId = publicationId;
            this.viewModelKeyField = viewModelKeyField ?? "viewModelKey";
        }
        public IComponentTemplate GetComponentTemplate(ViewModelAttribute viewModelAttribute)
        {
            string viewModelKey = viewModelAttribute.ViewModelKeys == null ? null : viewModelAttribute.ViewModelKeys.FirstOrDefault(); //guess it's the first one
            FieldSet meta = new FieldSet();
            if (viewModelAttribute.ViewModelKeys != null && viewModelAttribute.ViewModelKeys.Length > 0) //only add the meta value if there is something to add
            {
                meta.Add(viewModelKeyField,
                        new Field
                        {
                            Values = new List<string> { viewModelKey }
                        });
            }

            return new ComponentTemplate
            {
                Title = viewModelAttribute.ComponentTemplateName,
                MetadataFields = meta,
                Id = TridionHelper.GetRandomTcmUri(publicationId, 32)
            };
        }
    }

    internal class ComponentPresentationMocker : IComponentPresentationMocker
    {
        private const string defaultViewModelIdFieldName = "viewModelKey";
        private ICTMocker ctMocker;
        private int publicationId = 1;
        internal ComponentPresentationMocker(ICTMocker ctMocker, int publicationId)
        {
            this.publicationId = publicationId;
            if (ctMocker == null) throw new ArgumentNullException("ctMocker");
            this.ctMocker = ctMocker;
        }
        public IComponentPresentation ConvertToComponentPresentation(IDD4TViewModel viewModel) //For mocking DD4T objects
        {
            Type type = viewModel.GetType();
            ViewModelAttribute attr = ReflectionCache.GetViewModelAttribute(type);
            IComponentTemplate template = ctMocker.GetComponentTemplate(attr);
            IFieldSet metadataFields;
            IFieldSet fields = CreateFields(viewModel, type, template, out metadataFields);
            AddXpathToFields(fields, "tcm:Content/custom:Content");
            AddXpathToFields(metadataFields, "tcm:Metadata/custom:Metadata");
            IComponentPresentation result = new ComponentPresentation
            {
                Component = new Component
                {
                    Fields = (FieldSet)fields,
                    MetadataFields = (FieldSet)metadataFields,
                    Schema = new Schema { Title = attr.SchemaName, Id = TridionHelper.GetRandomTcmUri(1, 8) },
                    Id = TridionHelper.GetRandomTcmUri(publicationId)
                },
                ComponentTemplate = (ComponentTemplate)template
            };
            return result;
        }
        public IFieldSet ConvertToFieldSet(IDD4TViewModel viewModel, out string schemaName)
        {
            Type type = viewModel.GetType();
            ViewModelAttribute attr = ReflectionCache.GetViewModelAttribute(type);
            IComponentTemplate template = ctMocker.GetComponentTemplate(attr);
            IFieldSet metadataFields;
            schemaName = attr.SchemaName;
            return CreateFields(viewModel, type, template, out metadataFields);
        }
        public void AddXpathToFields(IFieldSet fieldSet, string baseXpath)
        {
            // add XPath properties to all fields
            try
            {
                foreach (Field f in fieldSet.Values)
                {
                    f.XPath = string.Format("{0}/custom:{1}", baseXpath, f.Name);
                    int i = 1;
                    if (f.EmbeddedValues != null)
                    {
                        foreach (FieldSet subFields in f.EmbeddedValues)
                        {
                            AddXpathToFields(subFields, string.Format("{0}/custom:{1}[{2}]", baseXpath, f.Name, i++));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string GetXmlRootName(IDD4TViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastPublishedDate(IComponentPresentationViewModel viewModel)
        {
            throw new NotImplementedException();
        }
        private IFieldSet CreateFields(object viewModel, Type type, IComponentTemplate template, out IFieldSet metadataFields)
        {
            IFieldSet fields;
            IFieldSet contentFields = new FieldSet();
            metadataFields = new FieldSet();
            var props = ReflectionCache.GetFieldProperties(type);
            Field field = new Field();
            FieldAttributeBase fieldAttribute;
            object fieldValue = null;
            foreach (var prop in props)
            {
                fieldAttribute = prop.FieldAttribute;//prop.GetCustomAttributes(typeof(FieldAttributeBase), true).FirstOrDefault() as FieldAttributeBase;
                if (fieldAttribute != null) //It has a FieldAttribute
                {
                    fields = fieldAttribute.IsMetadata ? metadataFields : contentFields; //switch between the two as needed
                    if (contentFields != null)
                    {
                        //TODO: Check the property type and make sure it matches expected return type or throw an exception -- not sure this is worth it
                        fieldValue = prop.Get(viewModel);
                        if (fieldValue != null)
                        {
                            try
                            {
                                field = (Field)fieldAttribute.SetFieldValue(fieldValue, prop.PropertyType, this);
                                field.Name = fieldAttribute.FieldName;
                            }
                            catch (Exception e)
                            {
                                if (e is TargetException || e is InvalidCastException)
                                    throw new InvalidCastException(
                                        String.Format("Type mismatch for property {0}. Expected type for {1} is {2}. Property is of type {3}."
                                        , prop.Name, fieldAttribute.GetType().Name, fieldAttribute.ExpectedReturnType.FullName, prop.PropertyType.FullName));
                                else throw e;
                            }
                            if (!fields.ContainsKey(fieldAttribute.FieldName))
                                fields.Add(fieldAttribute.FieldName, field);
                        }
                    }
                }
            }
            return contentFields;
        }
    }
}
