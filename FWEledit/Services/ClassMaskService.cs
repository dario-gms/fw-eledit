namespace FWEledit
{
    public sealed class ClassMaskService
    {
        public const int MaskBm = 1;
        public const int MaskWiz = 2;
        public const int MaskPsy = 4;
        public const int MaskVen = 8;
        public const int MaskBar = 16;
        public const int MaskAs = 32;
        public const int MaskAr = 64;
        public const int MaskCle = 128;
        public const int MaskSe = 256;
        public const int MaskMy = 512;
        public const int MaskDu = 1024;
        public const int MaskSt = 2048;

        public ClassMaskFlags GetFlagsFromMask(int mask)
        {
            return new ClassMaskFlags
            {
                IsBM = (mask & MaskBm) != 0,
                IsWiz = (mask & MaskWiz) != 0,
                IsPsy = (mask & MaskPsy) != 0,
                IsVen = (mask & MaskVen) != 0,
                IsBar = (mask & MaskBar) != 0,
                IsAs = (mask & MaskAs) != 0,
                IsAr = (mask & MaskAr) != 0,
                IsCle = (mask & MaskCle) != 0,
                IsSe = (mask & MaskSe) != 0,
                IsMy = (mask & MaskMy) != 0,
                IsDu = (mask & MaskDu) != 0,
                IsSt = (mask & MaskSt) != 0
            };
        }

        public int GetMaskFromFlags(ClassMaskFlags flags)
        {
            if (flags == null)
            {
                return 0;
            }

            int mask = 0;
            if (flags.IsBM) mask |= MaskBm;
            if (flags.IsWiz) mask |= MaskWiz;
            if (flags.IsPsy) mask |= MaskPsy;
            if (flags.IsVen) mask |= MaskVen;
            if (flags.IsBar) mask |= MaskBar;
            if (flags.IsAs) mask |= MaskAs;
            if (flags.IsAr) mask |= MaskAr;
            if (flags.IsCle) mask |= MaskCle;
            if (flags.IsSe) mask |= MaskSe;
            if (flags.IsMy) mask |= MaskMy;
            if (flags.IsDu) mask |= MaskDu;
            if (flags.IsSt) mask |= MaskSt;
            return mask;
        }
    }
}
