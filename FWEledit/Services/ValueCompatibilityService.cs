namespace FWEledit
{
    public sealed class ValueCompatibilityService
    {
        private readonly FieldValueValidationService fieldValueValidationService;

        public ValueCompatibilityService(FieldValueValidationService fieldValueValidationService)
        {
            this.fieldValueValidationService = fieldValueValidationService;
        }

        public bool IsValueCompatible(string fieldType, string value)
        {
            if (fieldValueValidationService == null)
            {
                return false;
            }
            return fieldValueValidationService.IsValueCompatible(fieldType, value);
        }
    }
}
