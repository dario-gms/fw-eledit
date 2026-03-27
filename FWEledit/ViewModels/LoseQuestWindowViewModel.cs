using System;

namespace FWEledit
{
    public sealed class LoseQuestWindowViewModel : ViewModelBase
    {
        private const string BaseUrl = "http://www.pwdatabase.com/pwi/quest/";

        public Uri BuildQuestUrl(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return null;
            }
            return new Uri(BaseUrl + questId);
        }
    }
}
