using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static Wcf.ProxyMonads.RestExtenssions;
using CommonExtensions;
using Wcf.ProxyMonads;

namespace Jira.Json {
  public class Field<T> {
    public T Value { protected get; set; }
    public Field field { get; set; }
    public object GetRawValue() { return this.Value; }
    public object GetJiraValue() { return Field.ValueFactory(Value, field.schema); }
    public object GetValueFromJira() {
      try {
        return Field.JiraValueFactory(Value, field.schema);
      } catch (Exception exc) {
        throw new Exception(new { fieldName = field.name } + "", exc);
      }
    }
    public override string ToString() {
      return GetJiraValue() + "";
    }
  }
  public partial class Field {
    public static Field<T> Create<T, U>(Field<U> field, T value) { return new Field<T> { field = field.field, Value = value }; }
    public static Field<T> Create<T>(Field field, T value) { return new Field<T> { field = field, Value = value }; }
    public static Field<T> Create<T>(T value) { return new Field<T> { field = new Field(), Value = value }; }
    // Extensions
    public partial class Schema {
      public string jiraType {
        get {
          return (this.custom ?? "").Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).DefaultIfEmpty("").Last();
        }
      }
    }

    // Methods
    public object ValueFactory(object value) {
      return ValueFactory(value, this.schema);
    }
    public static object ValueFactory<T>(T value, Field.Schema schema) {
      switch (schema.type) {
        case "string":
        case "option":
        switch (schema.jiraType) {
          case "readonlyfield":
          return string.IsNullOrEmpty(value + "") ? " " : value + "";
          case "select":
          case "radiobuttons": return new { value = ExtractValue(value) };
          case "multiselect":
          case "cascadingselect":
          var jValue = value as JObject;
          if (jValue == null) return value;
          var jp = jValue.Property("id");
          if (jp == null) throw new Exception("Multiselect value is missing 'id' property");
          return (jp.Value as JValue).Value;
          default: return value;
        }
        case "array":
        var type = schema.items;
        switch (schema.jiraType) {
          case "multiselect":
          var jArray = new[] { value as JArray }
            .Where(ja => ja != null)
            .Select(ja => ja.ToArray())
            .FirstOrDefault();
          var values = ((object[])(jArray ?? (value.GetType().IsArray ? (object)value : new[] { value }) ?? new object[0]))
            .Select(v => new { id = ValueFactory(v, new Field.Schema { type = schema.items, custom = schema.custom }) })
            .ToArray();
          return values;
          case "multicheckboxes":
          Passager.ThrowIf(() => !(value is string));
          var valuesCB = new[] { value + "" }
            .Where(s => !s.IsNullOrWhiteSpace())
            .Select(v => new { value })
            .ToArray();
          return valuesCB;
          default: return new { id = ExtractValue(value) };
        }
        case "option-with-child":
        switch (schema.jiraType) {
          case "cascadingselect":
          var values = ExtractValues(value);
          var exp = values.Take(1).Select(v => new { value = v }.ToExpando()).SingleOrDefault();
          values.Skip(1).Take(1).ForEach(child => exp = exp.Merge(new { child = new { value = child } }));
          return exp;
          default: throw new InvalidEnumArgumentException(new { schema = new { schema.type, schema.jiraType }, error = "JIRA type is not supported" } + "");
        }
        case "number": return ExtractValue(value);
        case "date": return ExtractDate(value)?.ToJiraDate() ?? "";
        case "datetime": return ExtractDateTime(value)?.ToJiraDateTime() ?? "";
        case "user": return User.FromUserName(ExtractValue(value) + "");
        case "group": return Group.FromUserName(ExtractValue(value) + "");
        case "issuetype": return new { name = value };
        default: throw new InvalidEnumArgumentException(new { schema = new { schema.type, schema.jiraType }, error = "JIRA type is not supported" } + "");
      }
    }
    public static object JiraValueFactory<T>(T value, Field.Schema schema) {
      switch (schema.type) {
        case "option":
        case "string":
        switch (schema.jiraType) {
          case "select":
          case "radiobuttons": return ExtractOption(value);
          case "multiselect":
          case "multicheckboxes":
          var jValue = value as JObject;
          if (jValue == null) return value;
          var jp = jValue.Property("value");
          if (jp == null) throw new Exception("Multiselect value is missing 'value' property");
          return (jp.Value as JValue).Value;
          default: return value;
        }
        case "array":
        var type = schema.items;
        switch (schema.jiraType) {
          case "multiselect":
          case "multicheckboxes":
          var jArray = new[] { value as JArray }
            .Where(ja => ja != null)
            .Select(ja => ja.ToArray())
            .FirstOrDefault();
          var values = ((object[])(jArray ?? (object)value ?? new object[0]))
            .Select(v => JiraValueFactory(v, new Field.Schema { type = schema.items, custom = schema.custom }))
            .ToArray();
          return values;
          default: return new { id = ExtractValue(value) };
        }
        case "option-with-child":
        switch (schema.jiraType) {
          case "cascadingselect":
          return ExtractCascadingOption(value);
          default: throw new InvalidEnumArgumentException(new { schema = new { schema.type, schema.jiraType }, error = "JIRA type is not supported" } + "");
        }
        case "number": return ExtractValue(value);
        case "date": return ExtractDate(value);
        case "datetime": return ExtractDateTime(value);
        case "user": return value == null ? null : User.FromObject(value).name;
        case "group": return value == null ? null : Group.FromObject(value).name;
        case "any":
        switch (schema.jiraType) {
          case "gh-lexo-rank": return value;
          default:
          return ThrowShemaTypeNotFound(value, schema);
        }
        default:
        return ThrowShemaTypeNotFound(value, schema);
      }
    }

    private static object ThrowShemaTypeNotFound<T>(T value, Schema schema) {
      if (value == null) return value;
      if (JiraMonad.JiraConfig.FieldTypesToIgnore.Any(ft => ft.ToLower() == schema.jiraType.ToLower())) return value;
      // Turn off InvalidEnumArgumentException
      return value;
      throw new InvalidEnumArgumentException(new { schema = new { schema.type, schema.jiraType }, error = "JIRA type is not supported" } + "");
    }

    // Statics
    static object ExtractOption<T>(T value) {
      if (value == null) return null;
      var jObject = value as JObject;
      Passager.ThrowIf(() => jObject == null);
      var jValue = jObject["value"] as JValue;
      Passager.ThrowIf(() => jValue == null);
      return jValue.Value + "";
    }
    static object[] ExtractCascadingOption<T>(T value) {
      if (value == null) return null;
      var jObject = value as JObject;
      Passager.ThrowIf(() => jObject == null);
      var jValue = jObject["value"] as JValue;
      Passager.ThrowIf(() => jValue == null);
      var jChild = jObject["child"] as JObject;
      var jChildValue = jChild?["value"] as JValue;
      var list = new[] { jValue.Value + "" };
      return jChildValue?.Value == null ? list : list.Concat(new[] { jChildValue.Value }).ToArray();
    }
    private static object[] ExtractValues<T>(T value) {
      if (value == null) return new object[0];
      var t = value as object[];
      return t == null ? new[] { value }.Cast<object>().ToArray() : t;
    }
    private static T ExtractValue<T>(T value) {
      var t = value as object[];
      return t == null ? value : t.Cast<T>().Single();
    }
    private static DateTime? ExtractDate<T>(T values) {
      var d = ExtractDateTime(values);
      return d.HasValue ? d.Value.Date : d;
    }
    private static DateTime? ExtractDateTime<T>(T value) {
      if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?)) return Convert.ToDateTime(value);
      var s = value + "";
      if (string.IsNullOrWhiteSpace(s)) return null;
      DateTime d;
      if (!DateTime.TryParse(value + "", out d)) throw new ArgumentException(new { value, error = "Is not DateTime" } + "");
      return d;
    }
    public string[] FilterValues() {
      return new[] { name, id }
      .Concat(clauseNames)
      .Where(s => !string.IsNullOrWhiteSpace(s))
      .ToArray();
    }
  }
}
