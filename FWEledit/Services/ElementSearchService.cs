using System;

namespace FWEledit
{
    public sealed class ElementSearchService
    {
        public ElementSearchResult FindNext(ElementSearchRequest request)
        {
            ElementSearchResult result = new ElementSearchResult();
            if (request == null || request.ListCollection == null || request.ListCollection.Lists == null)
            {
                return result;
            }

            string query = request.Query ?? string.Empty;
            if (!request.MatchCase)
            {
                query = query.ToLower();
            }
            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            if (request.SearchAllFields)
            {
                return FindInAllFields(request, query);
            }

            return FindByIdOrName(request, query);
        }

        private ElementSearchResult FindInAllFields(ElementSearchRequest request, string query)
        {
            ElementSearchResult result = new ElementSearchResult();
            eListCollection listCollection = request.ListCollection;
            int listCount = listCollection.Lists.Length;
            if (listCount == 0)
            {
                return result;
            }

            int startList = request.StartListIndex;
            if (startList < 0) { startList = 0; }
            if (startList >= listCount) { startList = listCount - 1; }

            int startElement = request.StartElementIndex;
            if (startElement < 0) { startElement = 0; }
            int startField = request.StartFieldIndex;
            if (startField < 0) { startField = 0; }

            int etmp = startElement;
            int ftmp = startField;
            for (int lf = startList; lf < listCount; lf++)
            {
                if (listCollection.Lists[lf] == null)
                {
                    etmp = 0;
                    ftmp = 0;
                    continue;
                }

                int elementCount = listCollection.Lists[lf].elementValues.Length;
                int fieldCount = listCollection.Lists[lf].elementFields.Length;
                for (int ef = etmp; ef < elementCount; ef++)
                {
                    for (int ff = ftmp; ff < fieldCount; ff++)
                    {
                        if (IsMatch(listCollection.GetValue(lf, ef, ff), query, request.ExactMatch, request.MatchCase))
                        {
                            result.Found = true;
                            result.ListIndex = lf;
                            result.ElementIndex = ef;
                            result.FieldIndex = ff;
                            return result;
                        }
                    }
                    ftmp = 0;
                }
                etmp = 0;
            }

            etmp = startElement;
            ftmp = startField;
            for (int lf = 0; lf < listCount && lf <= startList; lf++)
            {
                if (listCollection.Lists[lf] == null)
                {
                    etmp = 0;
                    ftmp = 0;
                    continue;
                }

                int elementCount = listCollection.Lists[lf].elementValues.Length;
                int fieldCount = listCollection.Lists[lf].elementFields.Length;
                for (int ef = etmp; ef < elementCount; ef++)
                {
                    for (int ff = ftmp; ff < fieldCount; ff++)
                    {
                        if (IsMatch(listCollection.GetValue(lf, ef, ff), query, request.ExactMatch, request.MatchCase))
                        {
                            result.Found = true;
                            result.ListIndex = lf;
                            result.ElementIndex = ef;
                            result.FieldIndex = ff;
                            return result;
                        }
                    }
                    ftmp = 0;
                }
                etmp = 0;
            }

            return result;
        }

        private ElementSearchResult FindByIdOrName(ElementSearchRequest request, string query)
        {
            ElementSearchResult result = new ElementSearchResult();
            eListCollection listCollection = request.ListCollection;
            int listCount = listCollection.Lists.Length;
            if (listCount == 0)
            {
                return result;
            }

            int startList = request.StartListIndex;
            if (startList < 0) { startList = 0; }
            if (startList >= listCount) { startList = listCount - 1; }

            int startElement = request.StartElementIndex;
            if (startElement < 0) { startElement = 0; }

            int etmp = startElement;
            for (int lf = startList; lf < listCount; lf++)
            {
                int pos = GetNameFieldIndex(listCollection, lf);
                int elementCount = listCollection.Lists[lf].elementValues.Length;
                for (int ef = etmp; ef < elementCount; ef++)
                {
                    if (IsMatchIdOrName(listCollection, lf, ef, pos, query, request.ExactMatch, request.MatchCase))
                    {
                        result.Found = true;
                        result.ListIndex = lf;
                        result.ElementIndex = ef;
                        return result;
                    }
                }
                etmp = 0;
            }

            etmp = startElement;
            for (int lf = 0; lf < listCount && lf <= startList; lf++)
            {
                int pos = GetNameFieldIndex(listCollection, lf);
                int elementCount = listCollection.Lists[lf].elementValues.Length;
                for (int ef = etmp; ef < elementCount; ef++)
                {
                    if (IsMatchIdOrName(listCollection, lf, ef, pos, query, request.ExactMatch, request.MatchCase))
                    {
                        result.Found = true;
                        result.ListIndex = lf;
                        result.ElementIndex = ef;
                        return result;
                    }
                }
                etmp = 0;
            }

            return result;
        }

        private int GetNameFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return 0;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 0;
        }

        private bool IsMatchIdOrName(eListCollection listCollection, int listIndex, int elementIndex, int nameFieldIndex, string query, bool exactMatch, bool matchCase)
        {
            if (exactMatch)
            {
                return string.Equals(listCollection.GetValue(listIndex, elementIndex, 0), query)
                    || string.Equals(listCollection.GetValue(listIndex, elementIndex, nameFieldIndex), query);
            }

            string nameValue = listCollection.GetValue(listIndex, elementIndex, nameFieldIndex);
            if (!matchCase)
            {
                nameValue = (nameValue ?? string.Empty).ToLower();
            }
            return string.Equals(listCollection.GetValue(listIndex, elementIndex, 0), query)
                || (nameValue ?? string.Empty).Contains(query);
        }

        private bool IsMatch(string value, string query, bool exactMatch, bool matchCase)
        {
            string rawValue = value ?? string.Empty;
            if (exactMatch)
            {
                return string.Equals(rawValue, query);
            }

            if (!matchCase)
            {
                rawValue = rawValue.ToLower();
            }
            return rawValue.Contains(query);
        }
    }
}
