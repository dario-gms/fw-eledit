using System;

namespace FWEledit
{
    public sealed class ReplaceService
    {
        public ReplaceResult Execute(eListCollection eLC, ReplaceParameters parameters)
        {
            ReplaceResult result = new ReplaceResult { Success = false };
            if (eLC == null)
            {
                result.Error = "List collection not loaded.";
                return result;
            }
            if (parameters == null)
            {
                result.Error = "Parameters not provided.";
                return result;
            }

            try
            {
                int replacementCount = 0;
                int l;
                int l_max;
                int e;
                int e_max;
                int v;
                int v_max;
                string oldValue = parameters.OldValue ?? string.Empty;
                string newValue = parameters.NewValue ?? string.Empty;
                string operation = parameters.Operation ?? string.Empty;
                decimal operand = parameters.Operand;

                if (parameters.ListIndexText == "*")
                {
                    l = 0;
                    l_max = eLC.Lists.Length;
                }
                else
                {
                    l = Convert.ToInt32(parameters.ListIndexText);
                    l_max = Convert.ToInt32(parameters.ListIndexText) + 1;
                    if (l < 0 || l_max > eLC.Lists.Length)
                    {
                        result.Error = "List Index Out of Range";
                        return result;
                    }
                }

                for (int lf = l; lf < l_max; lf++)
                {
                    if (parameters.ItemIndexText == "*")
                    {
                        e = 0;
                        e_max = eLC.Lists[lf].elementValues.Length;
                    }
                    else
                    {
                        e = Convert.ToInt32(parameters.ItemIndexText);
                        e_max = Convert.ToInt32(parameters.ItemIndexText) + 1;
                        if (e < 0 || e_max > eLC.Lists[lf].elementValues.Length)
                        {
                            result.Error = "Item Index Out of Range";
                            return result;
                        }
                    }

                    for (int ef = e; ef < e_max; ef++)
                    {
                        if (parameters.FieldIndexText == "*")
                        {
                            v = 0;
                            v_max = eLC.Lists[lf].elementValues[ef].Length;
                        }
                        else
                        {
                            v = Convert.ToInt32(parameters.FieldIndexText);
                            v_max = Convert.ToInt32(parameters.FieldIndexText) + 1;
                            if (v < 0 || v_max > eLC.Lists[lf].elementValues[ef].Length)
                            {
                                result.Error = "Field Index Out of Range";
                                return result;
                            }
                        }

                        for (int vf = v; vf < v_max; vf++)
                        {
                            if (parameters.ReplaceMode)
                            {
                                if (oldValue == "*" || oldValue == eLC.GetValue(lf, ef, vf).Trim(new char[] { Convert.ToChar(0) }))
                                {
                                    eLC.SetValue(lf, ef, vf, newValue);
                                    replacementCount++;
                                }
                            }
                            if (parameters.RecalculateMode)
                            {
                                string type = eLC.GetType(lf, vf);
                                string resultValue;
                                if (TryRecalculate(type, eLC.GetValue(lf, ef, vf), operation, operand, out resultValue))
                                {
                                    eLC.SetValue(lf, ef, vf, resultValue);
                                    replacementCount++;
                                }
                            }
                        }
                    }
                }

                result.Success = true;
                result.ReplacementCount = replacementCount;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        private static bool TryRecalculate(string type, string value, string operation, decimal operand, out string result)
        {
            result = null;
            if (type == "int16")
            {
                short op1 = Convert.ToInt16(value);
                short op2 = Convert.ToInt16(operand);
                result = ApplyOperation(op1, op2, operation).ToString();
                return true;
            }
            if (type == "int32")
            {
                int op1 = Convert.ToInt32(value);
                int op2 = Convert.ToInt32(operand);
                result = ApplyOperation(op1, op2, operation).ToString();
                return true;
            }
            if (type == "int64")
            {
                long op1 = Convert.ToInt64(value);
                long op2 = Convert.ToInt64(operand);
                result = ApplyOperation(op1, op2, operation).ToString();
                return true;
            }
            if (type == "float")
            {
                float op1 = Convert.ToSingle(value);
                float op2 = Convert.ToSingle(operand);
                result = ApplyOperation(op1, op2, operation).ToString();
                return true;
            }
            if (type == "double")
            {
                double op1 = Convert.ToDouble(value);
                double op2 = Convert.ToDouble(operand);
                result = ApplyOperation(op1, op2, operation).ToString();
                return true;
            }
            return false;
        }

        private static T ApplyOperation<T>(T left, T right, string operation) where T : struct
        {
            dynamic a = left;
            dynamic b = right;
            if (operation == "*")
            {
                return a * b;
            }
            if (operation == "/")
            {
                return a / b;
            }
            if (operation == "+")
            {
                return a + b;
            }
            if (operation == "-")
            {
                return a - b;
            }
            return a;
        }
    }
}
