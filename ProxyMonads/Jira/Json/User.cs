using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  [JsonObjectAttribute]
  public partial class User :IConvertible {
    public string self { get; set; }
    public string key { get; set; }
    public string name { get; set; }
    public string emailAddress { get; set; }
    public string displayName { get; set; }
    public bool active { get; set; }
    public string timeZone { get; set; }
    public Groups groups { get; set; }

    public class Groups {
      public int size { get; set; }
      public List<Item> items { get; set; }
    }
    public class Item {
      public string name { get; set; }
      public string self { get; set; }
    }

    public TypeCode GetTypeCode() {
      return name.GetTypeCode();
    }

    public bool ToBoolean(IFormatProvider provider) {
      return ((IConvertible)name).ToBoolean(provider);
    }

    public char ToChar(IFormatProvider provider) {
      return ((IConvertible)name).ToChar(provider);
    }

    public sbyte ToSByte(IFormatProvider provider) {
      return ((IConvertible)name).ToSByte(provider);
    }

    public byte ToByte(IFormatProvider provider) {
      return ((IConvertible)name).ToByte(provider);
    }

    public short ToInt16(IFormatProvider provider) {
      return ((IConvertible)name).ToInt16(provider);
    }

    public ushort ToUInt16(IFormatProvider provider) {
      return ((IConvertible)name).ToUInt16(provider);
    }

    public int ToInt32(IFormatProvider provider) {
      return ((IConvertible)name).ToInt32(provider);
    }

    public uint ToUInt32(IFormatProvider provider) {
      return ((IConvertible)name).ToUInt32(provider);
    }

    public long ToInt64(IFormatProvider provider) {
      return ((IConvertible)name).ToInt64(provider);
    }

    public ulong ToUInt64(IFormatProvider provider) {
      return ((IConvertible)name).ToUInt64(provider);
    }

    public float ToSingle(IFormatProvider provider) {
      return ((IConvertible)name).ToSingle(provider);
    }

    public double ToDouble(IFormatProvider provider) {
      return ((IConvertible)name).ToDouble(provider);
    }

    public decimal ToDecimal(IFormatProvider provider) {
      return ((IConvertible)name).ToDecimal(provider);
    }

    public DateTime ToDateTime(IFormatProvider provider) {
      return ((IConvertible)name).ToDateTime(provider);
    }

    public string ToString(IFormatProvider provider) {
      return name;
    }

    public object ToType(Type conversionType, IFormatProvider provider) {
      return ((IConvertible)name).ToType(conversionType, provider);
    }
  }
}