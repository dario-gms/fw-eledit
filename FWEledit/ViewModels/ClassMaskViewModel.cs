using System;

namespace FWEledit
{
    public sealed class ClassMaskViewModel : ViewModelBase
    {
        private readonly ClassMaskService classMaskService;
        private ClassMaskFlags flags = new ClassMaskFlags();
        private int maskValue;

        public ClassMaskViewModel(ClassMaskService classMaskService)
        {
            this.classMaskService = classMaskService ?? throw new ArgumentNullException(nameof(classMaskService));
        }

        public int MaskValue
        {
            get { return maskValue; }
        }

        public ClassMaskFlags Flags
        {
            get { return flags; }
        }

        public void UpdateFromMask(int mask)
        {
            maskValue = mask;
            flags = classMaskService.GetFlagsFromMask(mask);
        }

        public int UpdateFromFlags(ClassMaskFlags newFlags)
        {
            flags = newFlags ?? new ClassMaskFlags();
            maskValue = classMaskService.GetMaskFromFlags(flags);
            return maskValue;
        }
    }
}
