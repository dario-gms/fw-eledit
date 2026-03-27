namespace FWEledit
{
    public sealed class QuestOverflowService
    {
        public QuestOverflowReport BuildReport(eListCollection listCollection)
        {
            QuestOverflowReport report = new QuestOverflowReport();
            if (listCollection == null)
            {
                return report;
            }

            AppendOverflowItems(listCollection, 45, report.ReceiveItems);
            AppendOverflowItems(listCollection, 46, report.ActivateItems);

            return report;
        }

        private static void AppendOverflowItems(eListCollection listCollection, int listIndex, System.Collections.Generic.List<string> target)
        {
            if (listCollection.Lists.Length <= listIndex)
            {
                return;
            }

            for (int n = 0; n < listCollection.Lists[listIndex].elementValues.Length; n++)
            {
                bool isAddedElement = false;
                for (int f = 34; f < listCollection.Lists[listIndex].elementFields.Length; f++)
                {
                    string value = listCollection.GetValue(listIndex, n, f);
                    if (value != "0")
                    {
                        if (!isAddedElement)
                        {
                            target.Add("+++++ " + listCollection.GetValue(listIndex, n, 0) + " - " + listCollection.GetValue(listIndex, n, 1) + " +++++");
                            isAddedElement = true;
                        }
                        target.Add(value);
                    }
                }
            }
        }
    }
}
