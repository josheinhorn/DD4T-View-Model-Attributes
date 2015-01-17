﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ViewModels.Contracts;
using DD4T.ContentModel;
using System.Reflection;
using DD4T.Mvc.Html;
using System.Web.Mvc;
using DD4T.ViewModels.Reflection;

namespace DD4T.ViewModels.Attributes
{
    /// <summary>
    /// The Base class for all DD4T Field Attributes. Inherit this class to create custom attributes for decorating Domain View Models.
    /// </summary>
    public abstract class FieldAttributeBase : Attribute
    {
        protected readonly string fieldName;
        protected bool allowMultipleValues = false;
        protected bool inlineEditable = false;
        protected bool mandatory = false; //probably don't need this one
        protected bool isMetadata = false;
        /// <summary>
        /// Base Constructor
        /// </summary>
        /// <param name="fieldName">The Tridion schema field name for this property</param>
        public FieldAttributeBase(string fieldName)
        {
            this.fieldName = fieldName;
        }
        /// <summary>
        /// When overriden in a derived class, this method should return the value of the View Model property from a DD4T Field object
        /// </summary>
        /// <param name="field">The DD4T Field</param>
        /// <param name="propertyType">The concrete type of the view model property for this attribute</param>
        /// <param name="template">The Component Template to use</param>
        /// <param name="builder">The View Model Builder</param>
        /// <returns></returns>
        public abstract object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null);
        /// <summary>
        /// When overriden in a derived class, this method should return a DD4T field with the value appropriately set.
        /// </summary>
        /// <remarks>Used for mocking Component Presentation objects</remarks>
        /// <param name="value">Value of the property</param>
        /// <param name="propertyType">Concrete type of the property</param>
        /// <param name="mocker">Component Presentation Mocker</param>
        /// <returns></returns>
        public abstract IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker mocker = null);
        /// <summary>
        /// When overriden in a derived class, this property returns the expected return type of the View Model property.
        /// </summary>
        /// <remarks>Primarily used for debugging purposes. This property is used to throw an accurate exception at run time if
        /// the property return type does not match with the expected type.</remarks>
        public abstract Type ExpectedReturnType { get; }
        /// <summary>
        /// The Tridion schema field name for this property
        /// </summary>
        public string FieldName { get { return fieldName; } }
        /// <summary>
        /// Is a multi value field.
        /// </summary>
        public bool AllowMultipleValues
        {
            get
            {
                return allowMultipleValues;
            }
            set { allowMultipleValues = value; }
        }
        /// <summary>
        /// Is inline editable. For semantic use only.
        /// </summary>
        public bool InlineEditable
        {
            get
            {
                return inlineEditable;
            }
            set
            {
                inlineEditable = value;
            }
        }
        /// <summary>
        /// Is a mandatory field. For semantic use only.
        /// </summary>
        public bool Mandatory
        {
            get
            {
                return mandatory;
            }
            set
            {
                mandatory = value;
            }
        }
        /// <summary>
        /// Is a metadata field. False indicates this is a content field.
        /// </summary>
        public bool IsMetadata
        {
            get { return isMetadata; }
            set { isMetadata = value; }
        }
    }

    //Do we need a Metadata attribtue or should is just be a param of the Field Attribute?
    //public class MetadataFieldAttribute : FieldAttribute
    //{
    //    //Any key differences between Metadata and normal fields? Only difference is to use Component.MetadataField
    //    public MetadataFieldAttribute(string fieldName, SchemaFieldType fieldType)
    //        : base(fieldName, fieldType)
    //    { }
    //}

    /// <summary>
    /// A Component Link Field
    /// </summary>
    /// <example>
    /// To create a multi value linked component with a custom return Type:
    ///     [LinkedComponentField("content", LinkedComponentTypes = new Type[] { typeof(GeneralContentViewModel) }, AllowMultipleValues = true)]
    ///     public ViewModelList<GeneralContentViewModel> Content { get; set; }
    ///     
    /// To create a single linked component using the default DD4T type:
    ///     [LinkedComponentField("internalLink")]
    ///     public IComponent InternalLink { get; set; }
    /// </example>
    public class LinkedComponentFieldAttribute : FieldAttributeBase
    {
        protected Type[] linkedComponentTypes;

        /// <summary>
        /// A Linked Component Field
        /// </summary>
        /// <param name="fieldName">Tridion schema field name</param>
        public LinkedComponentFieldAttribute(string fieldName) : base(fieldName) { }
        /// <summary>
        /// The possible return types for this linked component field. Each of these types must implement the 
        /// return type of this property or its generic type if multi-value. If not used, the default DD4T
        /// Component object will be returned.
        /// </summary>
        public Type[] LinkedComponentTypes //Is there anyway to enforce the types passed to this?
        {
            get
            {
                return linkedComponentTypes;
            }
            set
            {
                linkedComponentTypes = value;
            }
        }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                if (linkedComponentTypes == null)
                {
                    fieldValue = field.LinkedComponentValues;
                }
                else
                {
                    //Property must implement IList<IComponentPresentationViewModel> -- use ComponentViewModelList<T>
                    IList<IDD4TViewModel> list =
                        (IList<IDD4TViewModel>)ReflectionCache.CreateInstance(propertyType);
                    foreach (var component in field.LinkedComponentValues)
                    {
                        list.Add(BuildLinkedComponent(field.LinkedComponentValues[0], template, builder));
                    }
                    fieldValue = list;
                }
            }
            else
            {
                fieldValue = linkedComponentTypes == null ? (object)field.LinkedComponentValues[0]
                    : (object)BuildLinkedComponent(field.LinkedComponentValues[0], template, builder);
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.ComponentLink };
            if (value != null)
            {
                field.LinkedComponentValues = new List<Component>();
                if (AllowMultipleValues)
                {

                    if (linkedComponentTypes == null)
                    {
                        field.LinkedComponentValues = (List<Component>)value;
                    }
                    else
                    {
                        //Property must implement IList<IComponentPresentationViewModel> -- use ComponentViewModelList<T>
                        IList<IDD4TViewModel> list = (IList<IDD4TViewModel>)value;
                        foreach (var model in list)
                        {
                            field.LinkedComponentValues.Add((Component)builder.ConvertToComponentPresentation(model).Component);
                        }
                    }
                }
                else
                {
                    field.LinkedComponentValues.Add(linkedComponentTypes == null ? (Component)value
                        : (Component)builder.ConvertToComponentPresentation((IDD4TViewModel)value).Component);
                }
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get
            {
                if (AllowMultipleValues)
                {
                    return linkedComponentTypes == null ? typeof(IList<IComponent>) : typeof(IList<IComponentPresentationViewModel>);
                }
                else
                {
                    return linkedComponentTypes == null ? typeof(IComponent) : typeof(IComponentPresentationViewModel);
                }
            }
        }
        private IComponentPresentationViewModel BuildLinkedComponent(IComponent component, IComponentTemplate template, IViewModelBuilder builder)
        {
            IComponentPresentation linkedCp = new ComponentPresentation
            {
                Component = component as Component,
                ComponentTemplate = template as ComponentTemplate
            };
            //need to determine schema to choose the Type
            Type type = GetViewModelType(component.Schema, template);
            //linkedModel = BuildCPViewModel(linkedType, linkedCp);
            return builder.BuildCPViewModel(type, linkedCp);
        }
        private Type GetViewModelType(ISchema schema, IComponentTemplate template = null)
        {
            //Create some algorithm to determine the proper view model type, perhaps build a static collection of all Types with the
            //View Model Attribute and set the key to the schema name + template name?
            if (schema == null) throw new ArgumentNullException("schema");
            //string ctName;
            foreach (var type in LinkedComponentTypes)
            {
                ViewModelAttribute modelAttr = ReflectionCache.GetViewModelAttribute(type);
                if (modelAttr != null && schema.Title == modelAttr.SchemaName) return type;

                //TODO: Possible include another type of marker besides CT to differentiate between different view models when using different template
                //ctName = template == null ? null : template.Title;
                //Compare CT and Schema -- this won't work for re-using component view models as both linked components and CPs until we have multiple CT names in attribute
                //if (modelAttr != null && new ViewModelKey(schema.Title, ctName).Equals(
                //    new ViewModelKey(modelAttr.SchemaName, modelAttr.ComponentTemplateName))) return type; 
            }
            return null;
        }
    }

    /// <summary>
    /// An embedded schema field
    /// </summary>
    public class EmbeddedSchemaFieldAttribute : FieldAttributeBase
    {
        protected Type embeddedSchemaType;
        /// <summary>
        /// Embedded Schema Field
        /// </summary>
        /// <param name="fieldName">The Tridion schema field name</param>
        /// <param name="embeddedSchemaType">The View Model type for this embedded field set</param>
        public EmbeddedSchemaFieldAttribute(string fieldName, Type embeddedSchemaType)
            : base(fieldName)
        {
            this.embeddedSchemaType = embeddedSchemaType;
        }
        public Type EmbeddedSchemaType
        {
            get
            {
                return embeddedSchemaType;
            }
        }
        //public EmbeddedSchemaFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                //Property must implement IList<IEmbeddedSchemaViewModel> -- use EmbeddedViewModelList<T>
                IList<IDD4TViewModel> list = (IList<IDD4TViewModel>)ReflectionCache.CreateInstance(propertyType);
                foreach (var fieldSet in field.EmbeddedValues)
                {
                    list.Add(builder.BuildEmbeddedViewModel(
                    EmbeddedSchemaType,
                    fieldSet, template));
                }
                fieldValue = list;
            }
            else
            {
                fieldValue = builder.BuildEmbeddedViewModel(EmbeddedSchemaType, field.EmbeddedValues[0], template);
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Embedded };
            string schemaName = string.Empty;
            if (value != null)
            {
                field.EmbeddedValues = new List<FieldSet>();
                if (AllowMultipleValues)
                {

                    if (embeddedSchemaType == null)
                    {
                        field.EmbeddedValues = (List<FieldSet>)value;
                    }
                    else
                    {
                        //Property must implement IList<IEmbeddedSchemaViewModel> -- use EmbeddedViewModelList<T>
                        IList<IDD4TViewModel> list = (IList<IDD4TViewModel>)value;

                        foreach (var model in list)
                        {
                            field.EmbeddedValues.Add((FieldSet)builder.ConvertToFieldSet(model, out schemaName));
                        }
                    }
                }
                else
                {
                    field.EmbeddedValues.Add(embeddedSchemaType == null ? (FieldSet)value
                        : (FieldSet)builder.ConvertToFieldSet((IDD4TViewModel)value, out schemaName));
                }
                field.EmbeddedSchema = new Schema { Title = schemaName };
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get { return AllowMultipleValues ? typeof(IList<IEmbeddedSchemaViewModel>) : typeof(IEmbeddedSchemaViewModel); }
        }
    }

    /// <summary>
    /// A Multimedia component field
    /// </summary>
    public class MultimediaFieldAttribute : FieldAttributeBase
    {
        public MultimediaFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                fieldValue = field.LinkedComponentValues.Select(v => v.Multimedia).ToList();
            }
            else
            {
                fieldValue = field.LinkedComponentValues[0].Multimedia;
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.MultiMediaLink };

            field.LinkedComponentValues = AllowMultipleValues ?
                ((List<IMultimedia>)value).Select(x => new Component { Multimedia = (Multimedia)x }).ToList()
                : new List<Component>() { new Component { Multimedia = (Multimedia)value } };

            return field;
        }
        public override Type ExpectedReturnType
        {
            get { return AllowMultipleValues ? typeof(IList<IMultimedia>) : typeof(IMultimedia); }
        }
    }

    /// <summary>
    /// A text field
    /// </summary>
    public class TextFieldAttribute : FieldAttributeBase, ICanBeBoolean
    {
        public TextFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                if (IsBooleanValue)
                    fieldValue = field.Values.Select(v => { bool b; return bool.TryParse(v, out b) && b; }).ToList();
                else fieldValue = field.Values;
            }
            else
            {
                if (IsBooleanValue)
                {
                    bool b;
                    fieldValue = bool.TryParse(field.Value, out b) && b;
                }
                else fieldValue = field.Value;
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.MultiMediaLink };
            if (AllowMultipleValues)
            {
                if (IsBooleanValue)
                {
                    field.Values = ((List<bool>)value).Select(x => x.ToString()).ToList();
                }
                else
                    field.Values = (List<string>)value;
            }
            else
            {
                field.Values = new List<string>() { value.ToString() };
            }
            return field;
        }
        /// <summary>
        /// Set to true to parse the text into a boolean value.
        /// </summary>
        public bool IsBooleanValue { get; set; }
        public override Type ExpectedReturnType
        {
            get
            {
                if (AllowMultipleValues)
                    return IsBooleanValue ? typeof(IList<bool>) : typeof(IList<string>);
                else return IsBooleanValue ? typeof(bool) : typeof(string);
            }
        }
    }

    /// <summary>
    /// A Rich Text field
    /// </summary>
    public class RichTextFieldAttribute : FieldAttributeBase
    {
        public RichTextFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                fieldValue = field.Values.Select(v => v.ResolveRichText()).ToList();
            }
            else
            {
                fieldValue = field.Value.ResolveRichText();
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Xhtml };
            if (AllowMultipleValues)
            {
                field.Values = ((List<MvcHtmlString>)value).Select(x => x.ToHtmlString()).ToList();
            }
            else
            {
                field.Values = new List<string>() { ((MvcHtmlString)value).ToHtmlString() };
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get { return AllowMultipleValues ? typeof(IList<MvcHtmlString>) : typeof(MvcHtmlString); }
        }
    }

    /// <summary>
    /// A Number field
    /// </summary>
    public class NumberFieldAttribute : FieldAttributeBase
    {
        public NumberFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                fieldValue = field.NumericValues;
            }
            else
            {
                fieldValue = field.NumericValues[0];
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Number };
            if (AllowMultipleValues)
            {
                field.NumericValues = (List<double>)value;
            }
            else
            {
                field.NumericValues = new List<double>() { (double)value };
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get { return AllowMultipleValues ? typeof(IList<double>) : typeof(double); }
        }

    }
    /// <summary>
    /// A Date/Time field
    /// </summary>
    public class DateFieldAttribute : FieldAttributeBase
    {
        public DateFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                fieldValue = field.DateTimeValues;
            }
            else
            {
                fieldValue = field.DateTimeValues[0];
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Date };
            if (AllowMultipleValues)
            {
                field.DateTimeValues = (List<DateTime>)value;
            }
            else
            {
                field.DateTimeValues = new List<DateTime>() { (DateTime)value };
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get { return AllowMultipleValues ? typeof(IList<DateTime>) : typeof(DateTime); }
        }
    }
    /// <summary>
    /// A Keyword field
    /// </summary>
    public class KeywordFieldAttribute : FieldAttributeBase
    {
        public KeywordFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object fieldValue = null;
            if (AllowMultipleValues)
            {
                fieldValue = field.Keywords;
            }
            else
            {
                fieldValue = field.Keywords[0];
            }
            return fieldValue;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Keyword };
            if (AllowMultipleValues)
            {
                field.Keywords = (List<Keyword>)value;
            }
            else
            {
                field.Keywords = new List<Keyword>() { (Keyword)value };
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get { return AllowMultipleValues ? typeof(IList<IKeyword>) : typeof(IKeyword); }
        }
    }

    /// <summary>
    /// The Key of a Keyword field. 
    /// </summary>
    public class KeywordKeyFieldAttribute : FieldAttributeBase, ICanBeBoolean
    {
        /// <summary>
        /// The Key of a Keyword field.
        /// </summary>
        /// <param name="fieldName">Tridion schema field name</param>
        public KeywordKeyFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object value = null;
            if (AllowMultipleValues)
            {
                if (IsBooleanValue)
                    value = field.Keywords.Select(k => { bool b; return bool.TryParse(k.Key, out b) && b; }).ToList();
                else value = field.Keywords.Select(k => k.Key);
            }
            else
            {
                if (IsBooleanValue)
                {
                    bool b;
                    value = bool.TryParse(field.Keywords[0].Key, out b) && b;
                }
                else value = field.Keywords[0].Key;
            }
            return value;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Keyword };
            if (AllowMultipleValues)
            {
                if (IsBooleanValue)
                {
                    field.Keywords = ((List<bool>)value).Select(x => new Keyword { Key = x.ToString() }).ToList();
                }
                else
                    field.Keywords = ((List<string>)value).Select(x => new Keyword { Key = x }).ToList();
            }
            else
            {
                field.Keywords = new List<Keyword>() { new Keyword { Key = value.ToString() } };
            }
            return field;
        }
        /// <summary>
        /// Set to true to parse the Keyword Key into a boolean value.
        /// </summary>
        public bool IsBooleanValue { get; set; }
        public override Type ExpectedReturnType
        {
            get
            {
                if (AllowMultipleValues)
                    return IsBooleanValue ? typeof(IList<bool>) : typeof(IList<string>);
                else return IsBooleanValue ? typeof(bool) : typeof(string);
            }
        }
    }

    public class NumericKeywordKeyFieldAttribute : FieldAttributeBase
    {
        public NumericKeywordKeyFieldAttribute(string fieldName) : base(fieldName) { }
        public override object GetFieldValue(IField field, Type propertyType, IComponentTemplate template, IViewModelBuilder builder = null)
        {
            object value = null;
            if (AllowMultipleValues)
            {
                value = field.Keywords.Select(k => { double i; double.TryParse(k.Key, out i); return i; }).ToList();
            }
            else
            {
                double i;
                double.TryParse(field.Keywords[0].Key, out i);
                value = i;
            }
            return value;
        }
        public override IField SetFieldValue(object value, Type propertyType, IComponentPresentationMocker builder = null)
        {
            Field field = new Field { FieldType = FieldType.Keyword };
            if (AllowMultipleValues)
            {
                field.Keywords = ((List<double>)value).Select(x => new Keyword { Key = x.ToString() }).ToList();
            }
            else
            {
                field.Keywords = new List<Keyword>() { new Keyword { Key = value.ToString() } };
            }
            return field;
        }
        public override Type ExpectedReturnType
        {
            get
            {
                return AllowMultipleValues ? typeof(IList<double>) : typeof(double);
            }
        }
    }

    //TODO: Use custom CT Metadata fields instead of CT Name
    /// <summary>
    /// A DD4T View Model.
    /// </summary>
    public class ViewModelAttribute : Attribute
    {
        private string schemaName;
        private bool inlineEditable = false;
        private string componentTemplateName;
        /// <summary>
        /// DD4T View Model
        /// </summary>
        /// <param name="schemaName">Tridion schema name for component type for this View Model</param>
        public ViewModelAttribute(string schemaName)
        {
            this.schemaName = schemaName;
        }

        public string SchemaName
        {
            get
            {
                return schemaName;
            }
        }
        /// <summary>
        /// The name of the Component Template. Use this to further specify a View model for specific presentations
        /// </summary>
        public string ComponentTemplateName //TODO: Use custom CT Metadata fields instead of CT Name
        {
            get { return componentTemplateName; }
            set { componentTemplateName = value; }
        }
        /// <summary>
        /// Is inline editable. Only for semantic use.
        /// </summary>
        public bool InlineEditable
        {
            get
            {
                return inlineEditable;
            }
            set
            {
                inlineEditable = value;
            }
        }

    }
}
