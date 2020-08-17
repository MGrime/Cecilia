using Eto.Forms;

namespace Trastevere
{
    public class MainForm : Form
    {
        public MainForm()
        {
            // Set window size
            ClientSize = new Eto.Drawing.Size(600,400);
            
            // Set title
            Title = "Trastevere";

            // Create menu bar
            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem
                    {
                        Text = "&File",
                        Items =
                        {
                            // you can add commands or menu items
                            new CustomCommand(),
                            // another menu item, not based off a Command
                            new ButtonMenuItem {Text = "Click Me, MenuItem"},
                            // Quit item
                            new Command((sender, args) => Application.Instance.Quit())
                            {
                              MenuText = "XAML cringe",
                              Shortcut = Application.Instance.CommonModifier | Keys.Q
                            },
                        }
                    }
                }
            };
        }

        private Cecilia_NET.Bot _myCeciliaBot;
    }
}