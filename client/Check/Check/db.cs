using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Net;
namespace Check
{
    class Db
    {
        private static string IPstr = "0";
        public static string connectionString = "server=vr;database=laptop;uid=teyhd;password=258000;";
        public static MyProg.IniFile MyIni = new MyProg.IniFile(@"C:\Windows\secur\settings.ini");
        public static int GetId()
        {
            
            var Lapnum = MyIni.Read("numb");
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                    return 0;
                }

                string sql = $"SELECT id FROM hosts WHERE lapid={Lapnum};";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Program.Mylog("ID: " + reader["id"].ToString());
                            return Int32.Parse(reader["id"].ToString());
                        }
                    }
                }

                connection.Close();
            }
            return 0;
        }

        [Obsolete]
        public static void InsertPC()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Program.Mylog(ex.ToString());
                }
                string Host = System.Net.Dns.GetHostName();
                string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
                var Lapnum = MyIni.Read("numb");
                string sql = $"INSERT INTO hosts (lapid,host,ip) VALUES ({Lapnum},'{Host}','{IP}');";
                Program.Mylog(sql);
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        [Obsolete]
        public static void UpdatePC()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {

                    Program.Mylog(ex.ToString());
                    // return 0;
                }
                string Host = System.Net.Dns.GetHostName();
                string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
                var Lapnum = MyIni.Read("numb");
                string sql = $"UPDATE hosts SET ip='{IP}',host='{Host}' WHERE lapid={Lapnum};";
                Program.Mylog(sql);
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine(reader.Read().ToString());
                        while (reader.Read())
                        {
                            Console.WriteLine(reader.ToString());
                            Program.Mylog(reader.ToString());
                        }
                    }
                }

                connection.Close();
            }

        }

        [Obsolete]
        public static void CheckHost()
        {
            string Host = System.Net.Dns.GetHostName();
            string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
            if (IP != IPstr)
            {
               if (GetId()==0)
                {
                    InsertPC();
                    Program.Mylog("Добавление ПК в базу");
                } else
                {
                    UpdatePC();
                    Program.Mylog("Обновление ПК в базе");
                }
            }
            if (IPstr == "0")
            {
                Program.Mylog("Первый запуск");
                IPstr = IP;
            }
        }
    }
}
