using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

using System.DirectoryServices;

namespace COVID_Test_handling
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public string address = @"huasql-001\NYIEvents\dbo.COVID19_Weekly_Test";
        public string externalAddress = @"huasql-001\NYIEvents\dbo.COVID19_Weekly_Test_ExternalData";
        public string rootFolder = @"\\huaapp-001\NYI_APPS\COVID19_Test";
        public string pwForDb = "LegoKIOSK4444";
        public string eventName = "";
        public string eventType = "";
        public string site = "";
        public string manualText = "";
        public string[] userData = new string[9];//userID,name,meal,photoAck
        public int time = 10;
        public string[,] userDataFromDB;
        public int userDataFromDB_length = 0;
        public int testActivity = 3; //set while the test is active -> have to be modifiable from config file
        public int datingBackCounter = 0;

        public void readConfig()
        {
            try
            {
                string[] config = System.IO.File.ReadAllLines(@"config.config");
                string[] delimiters = { "<eventName>", @"<\eventName>", @"<eventType>", @"<\eventType>", "<eventDB>", @"<\eventDB>", "<employeeDB>", @"<\employeeDB>", "<site>", @"<\site>" };
                foreach (string line in config)
                {
                    if (line.StartsWith(delimiters[0]))
                    {
                        eventName = line.Substring(line.IndexOf(">") + 1, line.Substring(line.IndexOf(">") + 1).LastIndexOf("<"));
                    }
                    else if (line.StartsWith(delimiters[2]))
                    {
                        eventType = line.Substring(line.IndexOf(">") + 1, line.Substring(line.IndexOf(">") + 1).LastIndexOf("<"));
                    }
                    else if (line.StartsWith(delimiters[4]))
                    {
                        address = line.Substring(line.IndexOf(">") + 1, line.Substring(line.IndexOf(">") + 1).LastIndexOf("<"));
                    }
                    else if (line.StartsWith(delimiters[8]))
                    {
                        site = line.Substring(line.IndexOf(">") + 1, line.Substring(line.IndexOf(">") + 1).LastIndexOf("<"));
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                sendErrorEmail(e.Source, e.Message + "\r\r" + e.StackTrace);
                this.Dispose();
            }
        }

        public void sendErrorEmail(string subject,string body)
        {
            //for every Exception where the variable declared az ex
            //sendErrorEmail(ex.Source, ex.Message + "\r\r"+ex.StackTrace);

            string hostname = Dns.GetHostName();

            var smtpClient = new SmtpClient("smtp.corp.lego.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("hu2kiosk@lego.com", "kjXH99saMM39"),
                EnableSsl = true,
            };
            smtpClient.Send("hu2kiosk@lego.com", "nyi_application_exceptions@o365.corp.LEGO.com", "Error email - (" + hostname + ") - " +
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "Sent by: " + subject + "\r\rException: " + body);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            panelManual.Enabled = false;
            panelSearch.Enabled = false;

            string[] lines = System.IO.File.ReadAllLines(rootFolder + @"\locationList.config");
            comboBox1.Items.Clear();
            foreach(string line in lines)
            {
                comboBox1.Items.Add(line);
            }


            if (Environment.MachineName != "HUC1455")
            {
                try
                {
                    serialPort1.Open();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Please attach card reader on " + serialPort1.PortName + "!");
                    sendErrorEmail(ex.Source, ex.Message + "\r\r"+ex.StackTrace);
                    this.Dispose();
                }
            }
        }

        private void panelManual_Click(object sender, EventArgs e)
        {
            manual manual = new manual();
            manual.ShowDialog();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            time--;
            if (time <= 0)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    //label3.Font = new Font("LEGO Chalet 60", 20, FontStyle.Bold | FontStyle.Italic);
                        label3.Text = "\r \rKérem a következő \rdolgozót!";
                    pictureBox1.Image = Properties.Resources.lamp_yellow;
                });
            }
        }

        public string[,] getDataFromDB(string userID)
        {
            string[,] returnTable = null;
            userDataFromDB_length = 0;
            /*this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Keresés a táblázatban...";
            });*/

            string server = address.Substring(0, address.IndexOf(@"\"));
            string db = address.Substring(server.Length + 1, address.Substring(server.Length + 1).IndexOf(@"\"));
            string tableName = address.Substring(server.Length + 1 + db.Length + 1);

            try
            {
                SqlConnection connection = new SqlConnection("Data Source=" + server + ";Initial Catalog = " + db + "; User Id=hu2kiosk; Password="+ pwForDb);
                SqlCommand command;
                connection.Open();

                command = new SqlCommand("SELECT COUNT(*) AS 'length' FROM " + tableName + " WHERE EmpID=@employeeNumber", connection);
                command.Parameters.AddWithValue("employeeNumber", userID);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        userDataFromDB_length = Convert.ToInt32(reader["length"].ToString());
                    }


                }

                returnTable = new string[userDataFromDB_length, 9];

                command = new SqlCommand("SELECT * FROM " + tableName + " WHERE EmpID=@employeeNumber ORDER BY Date", connection);
                command.Parameters.AddWithValue("employeeNumber", userID);

                int i = 0;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        returnTable[i, 0] = reader["ID"].ToString();
                        returnTable[i, 1] = reader["EmpID"].ToString();
                        returnTable[i, 2] = reader["Name"].ToString();
                        returnTable[i, 3] = reader["Place"].ToString();
                        returnTable[i, 4] = reader["Date"].ToString();
                        returnTable[i, 5] = reader["Time"].ToString();
                        returnTable[i, 6] = reader["Manager"].ToString();
                        returnTable[i, 7] = reader["Result"].ToString();
                        returnTable[i, 8] = reader["ManualEntry"].ToString();
                        i++;
                    }
                }
                /*
                this.Invoke((MethodInvoker)delegate
                {
                    time = 10;
                    timer1.Start();
                });*/

                return returnTable;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                sendErrorEmail(e.Source, e.Message + "\r\r" + e.StackTrace);
                this.Invoke((MethodInvoker)delegate
                {
                    label3.Text = "\r \rKérem a következő \rdolgozót!";
                });
                return returnTable;
            }
        }

        public DirectoryEntry createDirectoryEntry()
        {
            DirectoryEntry ldapConnection = new DirectoryEntry("LDAP://corp.lego.com");
            ldapConnection.Path = GetCurrentDomainPath();
            ldapConnection.AuthenticationType = AuthenticationTypes.Secure;
            return ldapConnection;
        }

        public string[] getManagerData(string empId)
        {
            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Vezető adatainak lekérése.";
            });

            string[] ret = new string[3];
            try
            {
                DirectoryEntry myLdapConnection = createDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(myLdapConnection);
                search.Filter = "(extensionAttribute3=" + empId + ")";

                SearchResultCollection results;
                results = search.FindAll();

                foreach (SearchResult sr in results)
                {
                    ret[0] = sr.Properties["Manager"][0].ToString();
                    ret[0] = ret[0].Substring(ret[0].IndexOf("=") + 1, ret[0].Substring(0, ret[0].IndexOf(",") - 3).Length);

                    myLdapConnection = createDirectoryEntry();
                    search = new DirectorySearcher(myLdapConnection);
                    search.Filter = "(Name=" + ret[0] + ")";
                    SearchResultCollection manResults;
                    manResults = search.FindAll();
                    foreach (SearchResult srMan in manResults)
                    {
                        try
                        {
                            ret[1] = srMan.Properties["mail"][0].ToString();
                            ret[2] = srMan.Properties["DisplayName"][0].ToString();
                        }
                        catch
                        {
                            ret[1] = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adatlekérdezési hiba történt az Active Directory-ból!");
                sendErrorEmail(ex.Source, ex.Message + "\r\r" + ex.StackTrace);
            }
            return ret;
        }


        public string GetCurrentDomainPath()
        {
            DirectoryEntry de = new DirectoryEntry("LDAP://RootDSE");

            return "LDAP://" + de.Properties["defaultNamingContext"][0].ToString();
        }

        public void searchInAD(string userID)
        {
            userData = new string[9];

            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r " + userID + " adatainak keresése";
            });
            try
            {
                DirectoryEntry myLdapConnection = createDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(myLdapConnection);
                search.Filter = "(extensionAttribute3=" + userID + ")";

                SearchResultCollection results;
                results = search.FindAll();

                foreach (SearchResult sr in results)
                {
                    userData[0] = sr.Properties["extensionAttribute3"][0].ToString();
                    userData[1] = sr.Properties["displayname"][0].ToString();
                    try
                    {
                        userData[2] = sr.Properties["mail"][0].ToString();
                    }
                    catch
                    {

                    }
                    try
                    {
                        userData[3] = sr.Properties["Department"][0].ToString();
                    }
                    catch
                    {

                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                sendErrorEmail(e.Source, e.Message + "\r\r" + e.StackTrace);
            }
        }

        public void writeToAdcDB(bool notLego,string legoMan)
        {
            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Adatok feltöltése az adatbázisba";
            });

            string temp = address;
            string server = temp.Substring(0, temp.IndexOf(@"\"));
            string db = temp.Substring(server.Length + 1, temp.Substring(server.Length + 1).IndexOf(@"\"));
            string tableName = temp.Substring(server.Length + 1 + db.Length + 1);
            string[] managerData = new string[3];
            if (!notLego)
            {
                managerData = getManagerData(userData[0]);
            }
            else
            {
                managerData[2] = legoMan;
            }
            try
            {
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/e5fa4f20-8293-4461-9fee-91867d4318ea/c-sql-insert-statement
                SqlConnection connection = new SqlConnection("Data Source=" + server + ";Initial Catalog = " + db + "; User Id=hu2kiosk; Password="+ pwForDb);

                SqlCommand Cmd = new SqlCommand("insert into " + tableName + "(EmpID,Name,Place,Date,Time,Manager,Result,ManualEntry,Company) values (@userID,@Name,@Place,@Date,@Time,@Manager,@Result,@ManualEntry,@Company)", connection);

                Cmd.Parameters.AddWithValue("@userID", userData[0]);
                Cmd.Parameters.AddWithValue("@Name", userData[1]);
                Cmd.Parameters.AddWithValue("@Place", comboBox1.SelectedItem.ToString());
                if(groupBox1.Visible == true)
                {
                    Cmd.Parameters.AddWithValue("@Date", manualDate.Text);
                    Cmd.Parameters.AddWithValue("@Time", manualTime.Text);
                }
                else
                {
                    Cmd.Parameters.AddWithValue("@Date", DateTime.Now.ToShortDateString());
                    Cmd.Parameters.AddWithValue("@Time", DateTime.Now.ToShortTimeString());
                }


                Cmd.Parameters.AddWithValue("@Manager", managerData[2]);
                Cmd.Parameters.AddWithValue("@ManualEntry", manualText);
                Cmd.Parameters.AddWithValue("@Result", "-");
                Cmd.Parameters.AddWithValue("@Company", userData[3]);

               connection.Open();

                int RowsAffected = Cmd.ExecuteNonQuery();//should be one!



                connection.Close();
                this.Invoke((MethodInvoker)delegate
                {
                    label3.Text = "\r \r" + userData[1] + "\r("+userData[0]+")\r tesztelése rögzítve!";
                    pictureBox1.Image = Properties.Resources.lamp_green;
                    time = 10;
                    timer1.Start();
                });

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                sendErrorEmail(e.Source, e.Message + "\r\r" + e.StackTrace);
            }
        }

        public void writeToAdcDB_External(string identityID,string name, string department, string title, string legoMan)
        {
            this.Invoke((MethodInvoker)delegate
            {
                label3.Text = "\r \r Adatok feltöltése az adatbázisba";
            });

            string temp = externalAddress;
            string server = temp.Substring(0, temp.IndexOf(@"\"));
            string db = temp.Substring(server.Length + 1, temp.Substring(server.Length + 1).IndexOf(@"\"));
            string tableName = temp.Substring(server.Length + 1 + db.Length + 1);

            
            //upload new data to External table:
            try
            {
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/e5fa4f20-8293-4461-9fee-91867d4318ea/c-sql-insert-statement
                SqlConnection connection = new SqlConnection("Data Source=" + server + ";Initial Catalog = " + db + "; User Id=hu2kiosk; Password=" + pwForDb);

                SqlCommand Cmd = new SqlCommand("insert into " + tableName + "(IdentityNum,Name,Department,Title,LegoMan) values (@userID,@Name,@Dep,@Title,@LegoMan)", connection);

                Cmd.Parameters.AddWithValue("@userID", identityID);
                Cmd.Parameters.AddWithValue("@Name", name);
                Cmd.Parameters.AddWithValue("@Dep", department);
                Cmd.Parameters.AddWithValue("@Title", title);
                Cmd.Parameters.AddWithValue("@LegoMan", legoMan);

                connection.Open();

                int RowsAffected = Cmd.ExecuteNonQuery();//should be one!



                connection.Close();
                this.Invoke((MethodInvoker)delegate
                {
                    label3.Text = "\r \r" + userData[1] + "\r("+userData[0]+")\r adatai rögzítve!";
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                sendErrorEmail(e.Source, e.Message + "\r\r" + e.StackTrace);
            }
            //upload the test registration:
            temp = address;
            server = temp.Substring(0, temp.IndexOf(@"\"));
            db = temp.Substring(server.Length + 1, temp.Substring(server.Length + 1).IndexOf(@"\"));
            tableName = temp.Substring(server.Length + 1 + db.Length + 1);

            try
            {
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/e5fa4f20-8293-4461-9fee-91867d4318ea/c-sql-insert-statement
                SqlConnection connection = new SqlConnection("Data Source=" + server + ";Initial Catalog = " + db + "; User Id=hu2kiosk; Password=" + pwForDb);

                SqlCommand Cmd = new SqlCommand("insert into " + tableName + "(EmpID,Name,Place,Date,Time,Manager,Result,ManualEntry,Company) values (@userID,@Name,@Place,@Date,@Time,@Manager,@Result,@ManualEntry,@Company)", connection);

                Cmd.Parameters.AddWithValue("@userID", identityID);
                Cmd.Parameters.AddWithValue("@Name", name);
                Cmd.Parameters.AddWithValue("@Place", comboBox1.SelectedItem.ToString());
                Cmd.Parameters.AddWithValue("@Date", DateTime.Now.ToShortDateString());
                Cmd.Parameters.AddWithValue("@Time", DateTime.Now.ToShortTimeString());
                Cmd.Parameters.AddWithValue("@Manager", legoMan);
                Cmd.Parameters.AddWithValue("@ManualEntry", "manuális");
                Cmd.Parameters.AddWithValue("@Result", "-");
                Cmd.Parameters.AddWithValue("@Company", department);

                connection.Open();

                int RowsAffected = Cmd.ExecuteNonQuery();//should be one!



                connection.Close();
                this.Invoke((MethodInvoker)delegate
                {
                    label3.Text = "\r \r" + name + "\r(" + identityID + ")\r tesztelése rögzítve!";
                    pictureBox1.Image = Properties.Resources.lamp_green;
                    time = 10;
                    timer1.Start();
                });

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                sendErrorEmail(e.Source, e.Message + "\r\r" + e.StackTrace);
            }
        }

        public void reader_manual(string userID,string[] userDataFromManual)
        {
            manualText = "manuális";
            userDataFromDB = getDataFromDB(userID);

            //opciók: van érvényes tesztje, nincs érvényes tesztje, nincs még tesztje
            if (userDataFromDB_length > 0)
            {
                //if the actual user has records
                int gotDay = Convert.ToDateTime(userDataFromDB[userDataFromDB_length - 1, 4]).AddDays(testActivity).DayOfYear;
                int toDay = DateTime.Now.DayOfYear;
                if (gotDay <= toDay)
                {
                    //if the test expired, new test needed to be uploaded to db:
                    userData[0] = userDataFromManual[0];
                    userData[1] = userDataFromManual[1];
                    userData[2] = userDataFromManual[2];
                    userData[3] = userDataFromManual[3];
                    userData[4] = userDataFromManual[4];
                    writeToAdcDB(true, userData[4]);
                }
                else
                {
                    //no new test needed. pushing latest data about the test
                    this.Invoke((MethodInvoker)delegate
                    {
                        label3.Text = "\r A legutóbbi érvényes tesztelés időpontja: \r" + userDataFromDB[userDataFromDB_length - 1, 4] + " " + userDataFromDB[userDataFromDB_length - 1, 5] + "\r Nem szükséges tesztelés!";
                    });
                    pictureBox1.Image = Properties.Resources.lamp_red;
                    time = 10;
                    timer1.Start();
                }
            }
            else
            {
                //if the actual user has no records
                //searchInAD(userID);
                userData[0] = userDataFromManual[0];
                userData[1] = userDataFromManual[1];
                userData[2] = userDataFromManual[2];
                userData[3] = userDataFromManual[3];
                userData[4] = userDataFromManual[4];
                writeToAdcDB(false, userData[4]);
            }
        }

        public void reader(string userID,bool fromManual)
        {
            if (userID.StartsWith("0"))
            {
                userID = userID.Substring(1);
            }
            if (fromManual)
            {
                manualText = "manuális";
            }
            else
            {
                manualText = "-";
            }
            //to remove the /R from the end of the string
            userID = userID.Substring(0, userID.Length - 1);

            userDataFromDB = getDataFromDB(userID);

            //opciók: van érvényes tesztje, nincs érvényes tesztje, nincs még tesztje
            if (userDataFromDB_length > 0)
            {
                //if the actual user has records
                int gotDay = Convert.ToDateTime(userDataFromDB[userDataFromDB_length - 1, 4]).AddDays(testActivity).DayOfYear;
                int toDay = DateTime.Now.DayOfYear;
                if (gotDay <= toDay)
                {
                    //if the test expired, new test needed to be uploaded to db:
                    searchInAD(userID);
                    writeToAdcDB(false, "");
                }
                else
                {
                    //no new test needed. pushing latest data about the test
                    this.Invoke((MethodInvoker)delegate
                    {
                        label3.Text = "\r A legutóbbi érvényes tesztelés időpontja: \r"+ userDataFromDB[userDataFromDB_length - 1, 4]+" "+ userDataFromDB[userDataFromDB_length - 1, 5]+"\r Nem szükséges tesztelés!";
                    });
                    pictureBox1.Image = Properties.Resources.lamp_red;
                    time = 10;
                    timer1.Start();
                }
            }
            else
            {
                //if the actual user has no records
                searchInAD(userID);
                writeToAdcDB(false, "");
            }
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                if (!comboBox1.SelectedItem.ToString().StartsWith("-"))
                {
                    string userid = serialPort1.ReadLine();
                    reader(userid, false);
                }
                else
                {
                    MessageBox.Show("Kérlek válassz stand nevet!");
                }
            });

            //clear buffer after reading:
            serialPort1.DiscardInBuffer();
        }

        private void panel2_Click(object sender, EventArgs e)
        {
            //---------for test session, triggering all of the process from here:---------//
            //reader("118962\r", false);
            datingBackCounter++;
            if(datingBackCounter == 5)
            {
                groupBox1.Visible = true;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!comboBox1.SelectedItem.ToString().StartsWith("-"))
            {
                comboBox1.Enabled = false;
                panelEditLocation.Visible = true;
                panelManual.Enabled = true;
                panelSearch.Enabled = true;
            }
        }

        private void panelEditLocation_Click(object sender, EventArgs e)
        {
            panelEditLocation.Visible = false;
            comboBox1.Enabled = true;
            panelManual.Enabled = false;
            panelSearch.Enabled = false;
        }

        private void panel5_Click(object sender, EventArgs e)
        {
            listHistory listHistory = new listHistory();
            listHistory.ShowDialog();
        }
    }
}
