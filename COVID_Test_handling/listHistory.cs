using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COVID_Test_handling
{
    public partial class listHistory : Form
    {
        public listHistory()
        {
            InitializeComponent();
        }

        public string[,] userDataFromTable;
        Panel[] rowPanels;

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void listHistory_Load(object sender, EventArgs e)
        {
            textBoxUserId.Focus();
        }

        private void panelSearchByID_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < rowPanels.Length; i++)
                {
                    rowPanels[i].Dispose();
                }
            }
            catch
            {

            }

            userDataFromTable = (System.Windows.Forms.Application.OpenForms["Form1"] as Form1).getDataFromDB(textBoxUserId.Text);
            if(userDataFromTable.GetLength(0) > 0)
            {
                rowPanels = new Panel[userDataFromTable.GetLength(0)];
                for (int i=0;i<userDataFromTable.GetLength(0); i++)
                {
                    Panel panel = new Panel();
                    panel.Size = new Size(mainPanel.Size.Width-30,30);
                    panel.Location = new Point(5, (30 * i) + (5* i)+35);
                    panel.BackColor = Color.FromArgb(255, 128, 0);
                    panel.Name = "row_" + i;
                    rowPanels[i] = panel;
                    mainPanel.Controls.Add(panel);

                    Label nameLabel = new Label();
                    nameLabel.Size = label12.Size;
                    nameLabel.Font = label12.Font;
                    nameLabel.Text = userDataFromTable[i, 2];
                    nameLabel.Location = new Point(0, 0);
                    nameLabel.Name = "name_" + i;
                    rowPanels[i].Controls.Add(nameLabel);

                    Label placeLabel = new Label();
                    placeLabel.Size = new Size(350, 29);
                    placeLabel.Font = label12.Font;
                    placeLabel.Text = userDataFromTable[i, 3];
                    placeLabel.Location = new Point(250, 0);
                    placeLabel.Name = "place_" + i;
                    rowPanels[i].Controls.Add(placeLabel);

                    Label dateLabel = new Label();
                    dateLabel.Size = new Size(200, 29);
                    dateLabel.Font = label12.Font;
                    dateLabel.Text = userDataFromTable[i, 4] + " " + userDataFromTable[i, 5];
                    dateLabel.Location = new Point(600, 0);
                    dateLabel.Name = "date_" + i;
                    rowPanels[i].Controls.Add(dateLabel);

                    Label managerLabel = new Label();
                    managerLabel.Size = new Size(350, 29);
                    managerLabel.Font = label12.Font;
                    managerLabel.Text = userDataFromTable[i, 6];
                    managerLabel.Location = new Point(800, 0);
                    managerLabel.Name = "manager_" + i;
                    rowPanels[i].Controls.Add(managerLabel);

                }
            }
            else
            {
                
            }
        }

        private void textBoxUserId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                panelSearchByID_Click(sender, e);
            }
        }
    }
}
