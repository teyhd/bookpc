using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
namespace LastSecur
{
    static class Db
    {
        public static string connectionString = "server=vr;database=laptop;uid=teyhd;password=258000;";
        //SELECT story.autor, story.pass FROM story WHERE lapid=12 ORDER BY timestart DESC LIMIT 1;

        public static int Isauth()
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
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                    return 0;
                }          
                
                string sql = $"SELECT story.autor, story.timestop FROM story WHERE lapid={LastSecur.Program.getid()} ORDER BY timestart DESC LIMIT 1;";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (Int32.Parse(reader["timestop"].ToString()) !=0) return 0;
                            Console.WriteLine("Авторизация: "+reader["autor"].ToString());
                            Program.Mylog("Авторизация: " + reader["autor"].ToString());
                            return Int32.Parse(reader["autor"].ToString());
                        }
                    }
                }

                connection.Close();
            }
            return 0;
        }

        public static int GetPass()
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
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                    return 0;
                }

                string sql = $"SELECT story.pass FROM story WHERE lapid={LastSecur.Program.getid()} ORDER BY timestart DESC LIMIT 1;";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["pass"].ToString());
                            Program.Mylog(reader["pass"].ToString());
                            return Int32.Parse(reader["pass"].ToString());
                        }
                    }
                }

                connection.Close();
            }
            return 0;
        }

        public static int GetLoginPass(string login, string pass)
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
                    Console.WriteLine(ex.ToString());
                    return 0;
                }

                string sql = $"SELECT id from users WHERE login='{login}' AND pass='{pass}'";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["id"].ToString());
                            Program.Mylog(reader["id"].ToString());
                            return Int32.Parse(reader["id"].ToString());
                        }
                    }
                }

                connection.Close();
            }
            return 0;
        }

        public static void Take(int userid,int kab)
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
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                }
                Random random = new Random();
                int pass = random.Next(10000, 100000);
                int lapid = LastSecur.Program.getid();
                int timestart = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string sql = $"INSERT INTO story (userid,lapid,kab,timestart,komm,pass,autor) VALUES ({userid},{lapid},{kab},{timestart},'Замечаний нет',{pass},1);";
                Console.WriteLine(sql);
                Program.Mylog(sql);
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public static void AuthPC()
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
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                    // return 0;
                }

                string sql = $"UPDATE story SET autor=1 WHERE lapid={LastSecur.Program.getid()} AND timestop=0 ORDER BY timestart DESC LIMIT 1;";
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

        public static void Open(){

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string sql = "SELECT * FROM users";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["id"].ToString());
                        }
                    }
                }

                connection.Close();
            }
        }
    }
}

