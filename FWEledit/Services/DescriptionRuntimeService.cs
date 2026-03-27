namespace FWEledit
{
    public sealed class DescriptionRuntimeService
    {
        public string[] BuildRuntimeArray(DescriptionViewModel viewModel)
        {
            if (viewModel == null)
            {
                return new string[0];
            }
            return viewModel.BuildRuntimeArray();
        }
    }
}
