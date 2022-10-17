using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.LookAndFeel;
using DevExpress.Utils;

namespace MANSR_VIEWER
{
    public static class MyXtraMessageBox
    {
        const string DefaultCaption = "";
        const IWin32Window DefaultOwner = null;
        const MessageBoxButtons DefaultButtons = MessageBoxButtons.OK;
        const MessageBoxIcon DefaultIcon = MessageBoxIcon.None;
        const MessageBoxDefaultButton DefaultDefButton = MessageBoxDefaultButton.Button1;
        public static DialogResult Show(string text) { return Show(DefaultOwner, text, DefaultCaption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(IWin32Window owner, string text) { return Show(owner, text, DefaultCaption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(string text, string caption) { return Show(DefaultOwner, text, caption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(IWin32Window owner, string text, string caption) { return Show(owner, text, caption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) { return Show(DefaultOwner, text, caption, buttons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons) { return Show(owner, text, caption, buttons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) { return Show(DefaultOwner, text, caption, buttons, icon, DefaultDefButton); }
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) { return Show(owner, text, caption, buttons, icon, DefaultDefButton); }
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) { return Show(DefaultOwner, text, caption, buttons, icon, defaultButton); }
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            return Show(owner, text, caption, MessageBoxButtonsToDialogResults(buttons), MessageBoxIconToIcon(icon), MessageBoxDefaultButtonToInt(defaultButton), icon);
        }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, string text) { return Show(lookAndFeel, DefaultOwner, text, DefaultCaption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, IWin32Window owner, string text) { return Show(lookAndFeel, owner, text, DefaultCaption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, string text, string caption) { return Show(lookAndFeel, DefaultOwner, text, caption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, IWin32Window owner, string text, string caption) { return Show(lookAndFeel, owner, text, caption, DefaultButtons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, string text, string caption, MessageBoxButtons buttons) { return Show(lookAndFeel, DefaultOwner, text, caption, buttons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, IWin32Window owner, string text, string caption, MessageBoxButtons buttons) { return Show(lookAndFeel, owner, text, caption, buttons, DefaultIcon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) { return Show(lookAndFeel, DefaultOwner, text, caption, buttons, icon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) { return Show(lookAndFeel, owner, text, caption, buttons, icon, DefaultDefButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) { return Show(lookAndFeel, DefaultOwner, text, caption, buttons, icon, defaultButton); }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            return Show(lookAndFeel, owner, text, caption, MessageBoxButtonsToDialogResults(buttons), MessageBoxIconToIcon(icon), MessageBoxDefaultButtonToInt(defaultButton), icon);
        }
        static DialogResult[] MessageBoxButtonsToDialogResults(MessageBoxButtons buttons)
        {
            if (!Enum.IsDefined(typeof(MessageBoxButtons), buttons))
            {
                throw new InvalidEnumArgumentException("buttons", (int)buttons, typeof(DialogResult));
            }
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    return new DialogResult[] { DialogResult.OK };
                case MessageBoxButtons.OKCancel:
                    return new DialogResult[] { DialogResult.OK, DialogResult.Cancel };
                case MessageBoxButtons.AbortRetryIgnore:
                    return new DialogResult[] { DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore };
                case MessageBoxButtons.RetryCancel:
                    return new DialogResult[] { DialogResult.Retry, DialogResult.Cancel };
                case MessageBoxButtons.YesNo:
                    return new DialogResult[] { DialogResult.Yes, DialogResult.No };
                case MessageBoxButtons.YesNoCancel:
                    return new DialogResult[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel };
                default:
                    throw new ArgumentException("buttons");
            }
        }
        static Icon MessageBoxIconToIcon(MessageBoxIcon icon)
        {
            if (!Enum.IsDefined(typeof(MessageBoxIcon), icon))
            {
                throw new InvalidEnumArgumentException("icon", (int)icon, typeof(DialogResult));
            }
            switch (icon)
            {
                case MessageBoxIcon.None:
                    return null;
                case MessageBoxIcon.Error:
                    return SystemIcons.Error;
                case MessageBoxIcon.Exclamation:
                    return SystemIcons.Exclamation;
                case MessageBoxIcon.Information:
                    return SystemIcons.Information;
                case MessageBoxIcon.Question:
                    return SystemIcons.Question;
                default:
                    throw new ArgumentException("icon");
            }
        }
        static int MessageBoxDefaultButtonToInt(MessageBoxDefaultButton defButton)
        {
            if (!Enum.IsDefined(typeof(MessageBoxDefaultButton), defButton))
            {
                throw new InvalidEnumArgumentException("defaultButton", (int)defButton, typeof(DialogResult));
            }
            switch (defButton)
            {
                case MessageBoxDefaultButton.Button1:
                    return 0;
                case MessageBoxDefaultButton.Button2:
                    return 1;
                case MessageBoxDefaultButton.Button3:
                    return 2;
                default:
                    throw new ArgumentException("defaultButton");
            }
        }
        public static DialogResult Show(UserLookAndFeel lookAndFeel, IWin32Window owner, string text, string caption, DialogResult[] buttons, Icon icon, int defaultButton, MessageBoxIcon messageBeepSound)
        {
            XtraMessageBoxForm form = new XtraMessageBoxForm();
            form.Appearance.Font = MessageFont;
            return form.ShowMessageBoxDialog(new XtraMessageBoxArgs(lookAndFeel, owner, text, caption, buttons, icon, defaultButton));
        }
        public static DialogResult Show(IWin32Window owner, string text, string caption, DialogResult[] buttons, Icon icon, int defaultButton, MessageBoxIcon messageBeepSound)
        {
            return Show(null, owner, text, caption, buttons, icon, defaultButton, messageBeepSound);
        }

        static bool _AllowCustomLookAndFeel = false;
        public static bool AllowCustomLookAndFeel
        {
            get { return _AllowCustomLookAndFeel; }
            set { _AllowCustomLookAndFeel = value; }
        }

        public static Font _MessageFont = AppearanceObject.DefaultFont;
        public static Font MessageFont
        {
            get { return _MessageFont; }
            set { _MessageFont = value; }
        }
    }
}
