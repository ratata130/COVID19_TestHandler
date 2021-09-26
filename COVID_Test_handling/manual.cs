using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.DirectoryServices;
using System.Data.SqlClient;

namespace COVID_Test_handling
{
    public partial class manual : Form
    {
        public manual()
        {
            InitializeComponent();
        }



        public bool manualEnter = false;
        public string address = @"huasql-001\NYIEvents\dbo.COVID19_Weekly_Test_ExternalData";
        public string externalAddress = @"huasql-001\NYIEvents\dbo.COVID19_Weekly_Test_ExternalData";
        public string pwForDb = "LegoKIOSK2222";
        public bool external = false;


        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void manualUpload()
        {
            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Adatok feltöltése az adatbázisba";
            });
            /*
            string temp = address;
            string server = temp.Substring(0, temp.IndexOf(@"\"));
            string db = temp.Substring(server.Length + 1, temp.Substring(server.Length + 1).IndexOf(@"\"));
            string tableName = temp.Substring(server.Length + 1 + db.Length + 1);

            string[] managerData = getManagerData(userData[0]);

            try
            {
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/e5fa4f20-8293-4461-9fee-91867d4318ea/c-sql-insert-statement
                SqlConnection connection = new SqlConnection("Data Source=" + server + ";Initial Catalog = " + db + "; User Id=hu2kiosk; Password=" + pwForDb);

                SqlCommand Cmd = new SqlCommand("insert into " + tableName + "(EmpID,Name,Place,Date,Time,Manager,Result,ManualEntry) values (@userID,@Name,@Place,@Date,@Time,@Manager,@Result,@ManualEntry)", connection);

                Cmd.Parameters.AddWithValue("@userID", userData[0]);
                Cmd.Parameters.AddWithValue("@Name", userData[1]);
                Cmd.Parameters.AddWithValue("@Place", comboBox1.SelectedItem.ToString());
                Cmd.Parameters.AddWithValue("@Date", DateTime.Now.ToShortDateString());
                Cmd.Parameters.AddWithValue("@Time", DateTime.Now.ToShortTimeString());
                Cmd.Parameters.AddWithValue("@Manager", managerData[1]);
                Cmd.Parameters.AddWithValue("@ManualEntry", manualText);
                Cmd.Parameters.AddWithValue("@Result", "-");

                connection.Open();

                int RowsAffected = Cmd.ExecuteNonQuery();//should be one!



                connection.Close();
                this.Invoke((MethodInvoker)delegate
                {
                    label3.Text = "\r \r" + userData[1] + "\r tesztelése rögzítve!";
                    pictureBox1.Image = Properties.Resources.lamp_green;
                    time = 10;
                    timer1.Start();
                });

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            */
        }

        public DirectoryEntry createDirectoryEntry()
        {
            DirectoryEntry ldapConnection = new DirectoryEntry("LDAP://corp.lego.com");
            ldapConnection.Path = GetCurrentDomainPath();
            ldapConnection.AuthenticationType = AuthenticationTypes.Secure;
            return ldapConnection;
        }

        public string GetCurrentDomainPath()
        {
            DirectoryEntry de = new DirectoryEntry("LDAP://RootDSE");

            return "LDAP://" + de.Properties["defaultNamingContext"][0].ToString();
        }

        public string getManagerData(string empId)
        {
            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Vezető adatainak lekérése.";
            });

            string ret = "";
            try
            {
                DirectoryEntry myLdapConnection = createDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(myLdapConnection);
                search.Filter = "(extensionAttribute3=" + empId + ")";

                SearchResultCollection results;
                results = search.FindAll();

                foreach (SearchResult sr in results)
                {
                    ret = sr.Properties["Manager"][0].ToString();
                    ret = ret.Substring(ret.IndexOf("=") + 1, ret.Substring(0, ret.IndexOf(",") - 3).Length);

                    myLdapConnection = createDirectoryEntry();
                    search = new DirectorySearcher(myLdapConnection);
                    search.Filter = "(Name=" + ret + ")";
                    SearchResultCollection manResults;
                    manResults = search.FindAll();
                    foreach (SearchResult srMan in manResults)
                    {
                        try
                        {
                            ret = srMan.Properties["DisplayName"][0].ToString();
                        }
                        catch
                        {
                            ret = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adatlekérdezési hiba történt az Active Directory-ból!");
            }
            return ret;
        }

        private void panelSearchByID_Click(object sender, EventArgs e)
        {
            textBoxManualName.Visible = false;
            textBoxManualDep.Visible = false;
            textBoxManualLegoMan.Visible = false;
            label13.Visible = false;
            manualEnter = false;
            external = false;
            label7.Visible = true;
            label6.Text = "Terület:";
            labelTitle.Text = "";



            string[] userData = new string[9];

      

            try
            {
                DirectoryEntry myLdapConnection = createDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(myLdapConnection);
                search.Filter = "(extensionAttribute3=" + textBoxUserId.Text + ")";

                SearchResultCollection results;
                results = search.FindAll();

                if (results.Count > 0)
                {
                    foreach (SearchResult sr in results)
                    {
                        userData[0] = sr.Properties["extensionAttribute3"][0].ToString();
                        userData[1] = sr.Properties["displayname"][0].ToString();
                        userData[2] = sr.Properties["Department"][0].ToString();
                        userData[3] = sr.Properties["Title"][0].ToString();
                        userData[4] = getManagerData(textBoxUserId.Text);
                        labelLegoMan.Text = userData[4];
                    }

                    labelName.Text = userData[1];
                    labelDepartment.Text = userData[2];
                    labelTitle.Text = userData[3];
                    panelData.Visible = true;
                }
                else
                {
                    string[] externalData = searchInExternal(textBoxUserId.Text);

                    if (externalData[0] == null)
                    {
                        manualEnter = true;
                        textBoxManualName.Visible = true;
                        textBoxManualDep.Visible = true;
                        textBoxManualLegoMan.Visible = true;
                        label13.Visible = true;
                        panelData.Visible = true;

                        label7.Visible = false;
                        label6.Text = "Cégnév:";
                    }
                    else
                    {
                        labelName.Text = externalData[1];
                        labelDepartment.Text = externalData[2];
                        labelTitle.Text = externalData[3];
                        labelLegoMan.Text = externalData[4];
                        label13.Visible = true;
                        labelLegoMan.Visible = true;
                        panelData.Visible = true;
                    }
                }
                
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }

        }

        public string[] searchInExternal(string userID)
        {
            string[] returnTable = new string[5];

            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Keresés a külsős táblázatban...";
            });

            string server = externalAddress.Substring(0, address.IndexOf(@"\"));
            string db = address.Substring(server.Length + 1, address.Substring(server.Length + 1).IndexOf(@"\"));
            string tableName = address.Substring(server.Length + 1 + db.Length + 1);

            try
            {
                SqlConnection connection = new SqlConnection("Data Source=" + server + ";Initial Catalog = " + db + "; User Id=hu2kiosk; Password=" + pwForDb);
                SqlCommand command;
                connection.Open();

                command = new SqlCommand("SELECT * FROM " + tableName + " WHERE IdentityNum=@employeeNumber", connection);
                command.Parameters.AddWithValue("employeeNumber", userID);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        returnTable[0] = reader["IdentityNum"].ToString();
                        returnTable[1] = reader["Name"].ToString();
                        returnTable[2] = reader["Department"].ToString();
                        returnTable[3] = reader["Title"].ToString();
                        returnTable[4] = reader["LegoMan"].ToString();
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return returnTable;
        }


        private void panelUploadData_Click(object sender, EventArgs e)
        {
            
            if (!manualEnter)
            {
                (System.Windows.Forms.Application.OpenForms["Form1"] as Form1).reader_manual(textBoxUserId.Text, new string[] { textBoxUserId.Text, labelName.Text, labelDepartment.Text, labelTitle.Text, labelLegoMan.Text });
                this.Close();
            }
            else
            {
                if (textBoxManualName.Text.Length > 0 & textBoxManualDep.Text.Length > 0 & textBoxManualLegoMan.Text.Length > 0)
                {
                    (System.Windows.Forms.Application.OpenForms["Form1"] as Form1).writeToAdcDB_External(textBoxUserId.Text, textBoxManualName.Text, textBoxManualDep.Text, "külsős alkalmazott", textBoxManualLegoMan.Text);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Minden adat kitöltése kötelező!");
                }
            }
        }

        private void textBoxUserId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                panelSearchByID_Click(sender, e);
            }
        }

        private void manual_Load(object sender, EventArgs e)
        {

        }
    }
}
