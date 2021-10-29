using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;


namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private static byte[] ReadInputRegistersMsg(ushort id, byte slaveAddress, ushort startAddress, byte function, int numberOfPoints) // Data read function
        {
            byte[] frame = new byte[12];
            frame[0] = (byte)(id );	// T₧ransaction Identifier High
            frame[1] = (byte)id; // Transaction Identifier Low
            frame[2] = 0; // Protocol Identifier High
            frame[3] = 0; // Protocol Identifier Low
            frame[4] = 0; // Message Length High
            frame[5] = 6; // Message Length Low(6 bytes to follow)
            frame[6] = slaveAddress; // Slave address(Unit Identifier)
            frame[7] = function; // Function             
            frame[8] = (byte)(startAddress >> 8); // Starting Address High
            frame[9] = (byte)startAddress; // Starting Address Low           
            frame[10] = (byte)(numberOfPoints >> 8); // Quantity of Registers High
            frame[11] = (byte)numberOfPoints; // Quantity of Registers Low
            return frame;
        }
        public Form1()
        {
            InitializeComponent();
            string connectionString = "Data Source = 'Your_DB_IP'; User='Your_DB_Username';Password='Your_DB_Password';Initial Catalog = 'Your_DB_Name'; Integrated Security = false";
            string ip = "10.3.9.249"; // Advantech IP
            int port = 502; // Port
            Ping ping = new Ping();
            var result = ping.Send(ip, 500);
            if (result.Status == IPStatus.Success) // Checking if we see our Advantech(controller) 
            {
                using (SqlConnection sqlCon = new SqlConnection(connectionString))
                {
                    using (TcpClient myClient = new TcpClient(ip, port)) // Connection to our IP
                    using (BufferedStream myStream = new BufferedStream(myClient.GetStream())) // Open stream
                    {
                        while (true) 
                        {
                            
                            sqlCon.Open();
                            string command_text = "SELECT * FROM r_temp_humidity_sensor"; // Select sensors info from our DB (Sensor_ID...etc)
                            SqlDataAdapter da1 = new SqlDataAdapter(command_text, sqlCon);
                            DataTable dt1 = new DataTable();
                            da1.Fill(dt1);
                            int row_count = dt1.Rows.Count;
                            int res = 0;
                            int s_id = 82; // Sensor_id by default
                            Int32 bytes = 0;
                            ushort s_addr = 3006; // Temperature read Code (3007 - humidity)
                            byte function_read = 4; // read funciton in modbus
                            byte slave_id = Convert.ToByte(s_id.ToString());
                            byte numOfRegs = 1;
                            byte[] sendData = new byte[24];
                            byte[] resData = new byte[24];
                            for (int i = 0; i < row_count; i++)
                            {
                                s_id = Convert.ToUInt16(dt1.Rows[i]["unit_id"].ToString());
                                bytes = 0;
                                s_addr = Convert.ToUInt16(dt1.Rows[i]["start_address"].ToString());
                                function_read = (byte)Convert.ToUInt16(dt1.Rows[i]["function_type"].ToString());
                                slave_id = Convert.ToByte(s_id.ToString());
                                numOfRegs = 1;

                                sendData = new byte[24];
                                resData = new byte[24];

                                sendData = ReadInputRegistersMsg(slave_id, slave_id, s_addr, function_read, numOfRegs); // Data read function
                                myStream.Write(sendData, 0, sendData.Length);
                                bytes = myStream.Read(resData, 0, resData.Length);
                                String results = String.Empty;
                                results = String.Format("{0:X2}", resData[9]) + String.Format("{0:X2}", resData[10]);
                                res = Convert.ToInt32(results, 16);
                                string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                                // Updating DB with new data.
                                // After that we easily will read data from FrontEnd
                                SqlCommand cmd = new SqlCommand("UPDATE r_temp_humidity_sensor SET qiymet  = " + res + ", tarix = '" + dateTimeNow + "' WHERE unit_id = " + s_id + " and start_address = " + s_addr + " ", sqlCon);
                                cmd.ExecuteNonQuery();
                            }
                            sqlCon.Close();
                            System.Threading.Thread.Sleep(3000);
                        }
                    }
                }
            }
            else
            {
                // Closing application if ping not reacheable
                if (System.Windows.Forms.Application.MessageLoop)
                {
                    System.Windows.Forms.Application.Exit();
                }
                else
                {
                    System.Environment.Exit(1);
                }
            }

        }

    }
}
