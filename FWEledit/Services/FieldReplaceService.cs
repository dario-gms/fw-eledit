using System;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class FieldReplaceService
    {
        public FieldReplaceResult Execute(
            eListCollection source,
            eListCollection other,
            eListConversation sourceConversation,
            eListConversation otherConversation,
            FieldReplaceRequest request,
            Action progressTick)
        {
            FieldReplaceResult result = new FieldReplaceResult { Success = false };
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
                string logPath = Path.Combine(request.LogDirectory, "Field_Replace.txt");
                using (StreamWriter sw = new StreamWriter(logPath, false, Encoding.Unicode))
                {
                }

                int l = request.ListIndex;
                int lo = request.ListIndex;
                int fieldIndex = request.FieldIndex;
                int fieldIndexOther = -1;
                int replacedCount = 0;

                if (l != source.ConversationListIndex)
                {
                    if (source.Version < 191 && other.Version >= 191 && request.ListIndex >= source.ConversationListIndex)
                    {
                        l++;
                    }
                    if (source.Version >= 191 && other.Version < 191 && request.ListIndex >= other.ConversationListIndex)
                    {
                        lo++;
                    }
                    if (l < source.Lists.Length && lo < other.Lists.Length)
                    {
                        for (int i = 0; i < other.Lists[lo].elementFields.Length; i++)
                        {
                            if (source.Lists[l].elementFields[fieldIndex] == other.Lists[lo].elementFields[i])
                            {
                                fieldIndexOther = i;
                                break;
                            }
                        }
                        if (fieldIndexOther > -1)
                        {
                            File.AppendAllText(logPath, "List: " + source.Lists[l].listName + " | Field: " + fieldIndex + " - " + source.Lists[l].elementFields[fieldIndex] + "\r\n\r\n\r\n", Encoding.Unicode);
                            for (int i = 0; i < source.Lists[l].elementValues.Length && i < other.Lists[lo].elementValues.Length; i++)
                            {
                                for (int io = 0; io < other.Lists[lo].elementValues.Length; io++)
                                {
                                    if (source.GetValue(l, i, 0) == other.GetValue(lo, io, 0))
                                    {
                                        if (source.GetValue(l, i, fieldIndex) != other.GetValue(lo, io, fieldIndexOther))
                                        {
                                            File.AppendAllText(logPath, "REPLACED\t\tID:" + source.GetValue(l, i, 0) + "\t\tValue: " + source.GetValue(l, i, fieldIndex) + "(by: " + other.GetValue(lo, io, fieldIndexOther) + ")\r\n", Encoding.Unicode);
                                            source.Lists[l].elementValues[i][fieldIndex] = other.Lists[lo].elementValues[io][fieldIndexOther];
                                            replacedCount++;
                                        }
                                        else
                                        {
                                            File.AppendAllText(logPath, "NO REPLACED\t\tID:" + source.GetValue(l, i, 0) + "\t\tValue: " + source.GetValue(l, i, fieldIndex) + "\r\n", Encoding.Unicode);
                                        }
                                        progressTick?.Invoke();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (sourceConversation != null && otherConversation != null)
                    {
                        for (int t = 0; t < sourceConversation.talk_proc_count; t++)
                        {
                            for (int to = 0; to < otherConversation.talk_proc_count; to++)
                            {
                                if (sourceConversation.talk_procs[t].id_talk == otherConversation.talk_procs[to].id_talk)
                                {
                                    for (int w = 0; w < sourceConversation.talk_procs[t].num_window; w++)
                                    {
                                        for (int wo = 0; wo < otherConversation.talk_procs[to].num_window; wo++)
                                        {
                                            if (sourceConversation.talk_procs[t].windows[w].id == otherConversation.talk_procs[to].windows[wo].id)
                                            {
                                                sourceConversation.talk_procs[t].windows[w].talk_text_len = otherConversation.talk_procs[to].windows[wo].talk_text_len;
                                                sourceConversation.talk_procs[t].windows[w].talk_text = otherConversation.talk_procs[to].windows[wo].talk_text;
                                                replacedCount++;
                                                for (int o = 0; o < sourceConversation.talk_procs[t].windows[w].num_option && o < otherConversation.talk_procs[to].windows[wo].num_option; o++)
                                                {
                                                    sourceConversation.talk_procs[t].windows[w].options[o].text = otherConversation.talk_procs[to].windows[wo].options[o].text;
                                                    replacedCount++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            progressTick?.Invoke();
                        }
                    }
                }

                result.Success = true;
                result.ReplacedCount = replacedCount;
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
