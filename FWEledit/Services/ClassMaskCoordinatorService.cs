using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ClassMaskCoordinatorService
    {
        public void HandleDecimalChanged(
            ClassMaskViewModel viewModel,
            NumericUpDown maskControl,
            ref bool lockCheckBox,
            CheckBox checkBoxBM,
            CheckBox checkBoxWiz,
            CheckBox checkBoxPsy,
            CheckBox checkBoxVen,
            CheckBox checkBoxBar,
            CheckBox checkBoxAs,
            CheckBox checkBoxAr,
            CheckBox checkBoxCle,
            CheckBox checkBoxSe,
            CheckBox checkBoxMy,
            CheckBox checkBoxDu,
            CheckBox checkBoxSt)
        {
            if (viewModel == null || maskControl == null)
            {
                return;
            }

            lockCheckBox = true;

            int number = Convert.ToInt32(maskControl.Value);
            viewModel.UpdateFromMask(number);
            ApplyFlagsToUi(
                viewModel.Flags,
                checkBoxBM,
                checkBoxWiz,
                checkBoxPsy,
                checkBoxVen,
                checkBoxBar,
                checkBoxAs,
                checkBoxAr,
                checkBoxCle,
                checkBoxSe,
                checkBoxMy,
                checkBoxDu,
                checkBoxSt);

            lockCheckBox = false;
        }

        public void HandleBinaryChanged(
            ClassMaskViewModel viewModel,
            NumericUpDown maskControl,
            ref bool lockCheckBox,
            CheckBox checkBoxBM,
            CheckBox checkBoxWiz,
            CheckBox checkBoxPsy,
            CheckBox checkBoxVen,
            CheckBox checkBoxBar,
            CheckBox checkBoxAs,
            CheckBox checkBoxAr,
            CheckBox checkBoxCle,
            CheckBox checkBoxSe,
            CheckBox checkBoxMy,
            CheckBox checkBoxDu,
            CheckBox checkBoxSt)
        {
            if (viewModel == null || maskControl == null)
            {
                return;
            }

            if (lockCheckBox)
            {
                return;
            }

            ClassMaskFlags flags = BuildFlagsFromUi(
                checkBoxBM,
                checkBoxWiz,
                checkBoxPsy,
                checkBoxVen,
                checkBoxBar,
                checkBoxAs,
                checkBoxAr,
                checkBoxCle,
                checkBoxSe,
                checkBoxMy,
                checkBoxDu,
                checkBoxSt);
            int number = viewModel.UpdateFromFlags(flags);
            maskControl.Value = Convert.ToDecimal(number);
        }

        private static ClassMaskFlags BuildFlagsFromUi(
            CheckBox checkBoxBM,
            CheckBox checkBoxWiz,
            CheckBox checkBoxPsy,
            CheckBox checkBoxVen,
            CheckBox checkBoxBar,
            CheckBox checkBoxAs,
            CheckBox checkBoxAr,
            CheckBox checkBoxCle,
            CheckBox checkBoxSe,
            CheckBox checkBoxMy,
            CheckBox checkBoxDu,
            CheckBox checkBoxSt)
        {
            return new ClassMaskFlags
            {
                IsBM = checkBoxBM.Checked,
                IsWiz = checkBoxWiz.Checked,
                IsPsy = checkBoxPsy.Checked,
                IsVen = checkBoxVen.Checked,
                IsBar = checkBoxBar.Checked,
                IsAs = checkBoxAs.Checked,
                IsAr = checkBoxAr.Checked,
                IsCle = checkBoxCle.Checked,
                IsSe = checkBoxSe.Checked,
                IsMy = checkBoxMy.Checked,
                IsDu = checkBoxDu.Checked,
                IsSt = checkBoxSt.Checked
            };
        }

        private static void ApplyFlagsToUi(
            ClassMaskFlags flags,
            CheckBox checkBoxBM,
            CheckBox checkBoxWiz,
            CheckBox checkBoxPsy,
            CheckBox checkBoxVen,
            CheckBox checkBoxBar,
            CheckBox checkBoxAs,
            CheckBox checkBoxAr,
            CheckBox checkBoxCle,
            CheckBox checkBoxSe,
            CheckBox checkBoxMy,
            CheckBox checkBoxDu,
            CheckBox checkBoxSt)
        {
            if (flags == null)
            {
                return;
            }

            checkBoxBM.Checked = flags.IsBM;
            checkBoxWiz.Checked = flags.IsWiz;
            checkBoxPsy.Checked = flags.IsPsy;
            checkBoxVen.Checked = flags.IsVen;
            checkBoxBar.Checked = flags.IsBar;
            checkBoxAs.Checked = flags.IsAs;
            checkBoxAr.Checked = flags.IsAr;
            checkBoxCle.Checked = flags.IsCle;
            checkBoxSe.Checked = flags.IsSe;
            checkBoxMy.Checked = flags.IsMy;
            checkBoxDu.Checked = flags.IsDu;
            checkBoxSt.Checked = flags.IsSt;
        }
    }
}
