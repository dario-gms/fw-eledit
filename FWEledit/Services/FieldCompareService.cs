using System;
using System.Collections;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class FieldCompareService
    {
        public FieldCompareResult Execute(eListCollection source, eListCollection other, FieldCompareRequest request, Action progressTick)
        {
            FieldCompareResult result = new FieldCompareResult { Success = false };
            if (source == null || other == null)
            {
                result.Error = "Elements data not loaded.";
                return result;
            }
            if (request == null)
            {
                result.Error = "Request not provided.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(request.LogDirectory))
            {
                result.Error = "Log directory not provided.";
                return result;
            }

            try
            {
                int l = request.ListIndex;
                if (l < 0 || l >= source.Lists.Length)
                {
                    result.Error = "List index out of range.";
                    return result;
                }

                ArrayList fields = new ArrayList();
                ArrayList excludes = new ArrayList();
                if (l != source.ConversationListIndex)
                {
                    for (int i = 0; i < source.Lists[l].elementFields.Length; i++)
                    {
                        if (request.ExcludedFieldIndices.Contains(i))
                        {
                            excludes.Add("\t#" + i + "\t- " + source.Lists[l].elementFields[i]);
                        }
                        else
                        {
                            fields.Add(source.Lists[l].elementFields[i]);
                        }
                    }

                    string logPath = Path.Combine(request.LogDirectory, "Field_Compare.txt");
                    using (StreamWriter sw = new StreamWriter(logPath, false, Encoding.Unicode))
                    {
                        sw.WriteLine("List: " + source.Lists[l].listName);
                        if (excludes.Count > 0)
                        {
                            sw.WriteLine("Fields Excluded:");
                            for (int i = 0; i < excludes.Count; i++)
                            {
                                sw.WriteLine(excludes[i]);
                            }
                        }
                    }

                    int lo = l;
                    if (source.Version < 191 && other.Version >= 191 && l >= other.ConversationListIndex)
                    {
                        lo--;
                    }
                    if (source.Version >= 191 && other.Version < 191 && l >= other.ConversationListIndex)
                    {
                        lo++;
                    }

                    for (int i = 0; i < source.Lists[l].elementValues.Length && i < other.Lists[lo].elementValues.Length; i++)
                    {
                        for (int eo = 0; eo < other.Lists[lo].elementValues.Length; eo++)
                        {
                            if (source.GetValue(l, i, 0) == other.GetValue(lo, eo, 0))
                            {
                                string log = string.Empty;
                                for (int fl = 0; fl < fields.Count; fl++)
                                {
                                    for (int fl1 = 0; fl1 < source.Lists[l].elementFields.Length; fl1++)
                                    {
                                        if (source.Lists[l].elementFields[fl1] == fields[fl].ToString())
                                        {
                                            for (int flo = 0; flo < other.Lists[lo].elementFields.Length; flo++)
                                            {
                                                if (other.Lists[lo].elementFields[flo] == fields[fl].ToString())
                                                {
                                                    if (source.GetValue(l, i, fl1) != other.GetValue(lo, eo, flo))
                                                    {
                                                        log += "Item ID: " + source.GetValue(l, i, 0) + "\tField: " + fields[fl] + "\tOld Value: " + source.GetValue(l, i, fl1) + "\tNew Value: " + other.GetValue(lo, eo, flo) + "\r\n";
                                                    }
                                                    break;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                                if (log != "")
                                {
                                    File.AppendAllText(logPath, "\r\n-------------------------------------------------------------------------\r\n" + log, Encoding.Unicode);
                                }
                                break;
                            }
                        }
                        progressTick?.Invoke();
                    }
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }
    }
}
