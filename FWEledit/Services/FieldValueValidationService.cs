using System;

namespace FWEledit
{
    public sealed class FieldValueValidationService
    {
        public bool IsValueCompatible(string fieldType, string value)
        {
            try
            {
                if (fieldType == null)
                {
                    return false;
                }
                if (fieldType == "int16")
                {
                    short tmp;
                    return short.TryParse(value, out tmp);
                }
                if (fieldType == "int32")
                {
                    int tmp;
                    return int.TryParse(value, out tmp);
                }
                if (fieldType == "int64")
                {
                    long tmp;
                    return long.TryParse(value, out tmp);
                }
                if (fieldType == "float")
                {
                    float tmp;
                    return float.TryParse(value, out tmp);
                }
                if (fieldType == "double")
                {
                    double tmp;
                    return double.TryParse(value, out tmp);
                }
                if (fieldType.Contains("byte:"))
                {
                    string[] hex = value.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (hex.Length == 0)
                    {
                        return false;
                    }
                    for (int i = 0; i < hex.Length; i++)
                    {
                        byte b;
                        if (!byte.TryParse(hex[i], System.Globalization.NumberStyles.HexNumber, null, out b))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
