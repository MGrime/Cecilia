using System;
using Eto.Forms;

namespace Trastevere
{
    public class CustomCommand : Command
    {
        public CustomCommand()
        {
            MenuText = "C&lick Me, Command";
            ToolBarText = "Click Me";
            ToolTip = "This shows a dialog for no reason";
            //Image = Icon.FromResource ("MyResourceName.ico");
            //Image = Bitmap.FromResource ("MyResourceName.png");
            Shortcut = Application.Instance.CommonModifier | Keys.M;  // control+M or cmd+M
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            MessageBox.Show(Application.Instance.MainForm, "You clicked me!", "Tutorial 2", MessageBoxButtons.OK);
        }
    }
}