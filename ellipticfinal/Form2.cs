using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Security.Cryptography;
using NetworksApi.TCP.SERVER;
using System.IO;

namespace ellipticfinal
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }



        static CngKey alice;

        static byte[] alicepublic;
        static byte[] reci1pubkeyblob;
        static byte[] reci2pubkeyblob;
        static byte[] reci3pubkeyblob;
        static byte[] encrypted1;
        static byte[] encrypted2;
        static byte[] encrypted3;

        private static void CreateKeys()
        {

            alice = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);

            alicepublic = alice.Export(CngKeyBlobFormat.EccPublicBlob);
        }

        private byte[] AliceSendsData(string message, byte[] publickey)
        {
            //Console.WriteLine("Alice sends message: {0}", message);
            byte[] rawData = Encoding.UTF8.GetBytes(message);
            byte[] encryptedData = null;

            using (var aliceAlgorithm = new ECDiffieHellmanCng(alice))
            using (CngKey bobPubKey = CngKey.Import(publickey,
                  CngKeyBlobFormat.EccPublicBlob))
            {
                byte[] symmKey = aliceAlgorithm.DeriveKeyMaterial(bobPubKey);

                Changetextboxcontents("symmkey::::" + Convert.ToBase64String(symmKey));
                
                //  Console.WriteLine("Alice creates this symmetric key with " +
                //      "Bobs public key information: {0}",
                //    Convert.ToBase64String(symmKey));

                using (var aes = new AesCryptoServiceProvider())
                {
                    aes.Key = symmKey;
                    aes.GenerateIV();
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // create CryptoStream and encrypt data to send
                        var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

                        // write initialization vector not encrypted
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        cs.Write(rawData, 0, rawData.Length);
                        cs.Close();
                        encryptedData = ms.ToArray();
                    }
                    aes.Clear();
                }
            }
            //Console.WriteLine("Alice: message is encrypted: {0}",
            //    Convert.ToBase64String(encryptedData)); ;
            //Console.WriteLine();
            return encryptedData;
        }


        public delegate void updatetext(string txt);
        
        void Changetextboxcontents(string txt)
        {
            if (textBox1.InvokeRequired)
            {
                Invoke(new updatetext(Changetextboxcontents), new object[] { txt });

            }
            else
            {
                textBox1.Text += txt + "\r\n";
            }
        }
        void Ftpeventtext(string txt)
        {
            if (textBox3.InvokeRequired)
            {
                Invoke(new updatetext(Ftpeventtext), new object[] { txt });

            }
            else
            {
                textBox3.Text += txt + "\r\n";
            }
        }
        Server chat;
        

        void chat_OnServerError(object Sender, ErrorArguments R)
        {
            MessageBox.Show(R.ErrorMessage + "\n" + R.Exception);
            Changetextboxcontents(R.ErrorMessage);
        }
        //string r1, r2, r3;
        void chat_OnDataReceived(object Sender, ReceivedArguments R)
        {
            if (R.Name == "reci1")
            {
                reci1pubkeyblob = Convert.FromBase64String(R.ReceivedData);
                Changetextboxcontents(R.Name + " key ::::::::" + R.ReceivedData +"\r\n");
                Changetextboxcontents("Alice public key:::::::"+ Convert.ToBase64String(alicepublic) + "\r\n" );
                chat.SendTo("reci1", Convert.ToBase64String(alicepublic));
                encrypted1 = AliceSendsData(textBox2.Text,reci1pubkeyblob);
                Changetextboxcontents("encrypted data1:" + Convert.ToBase64String(encrypted1) + "\r\n");
                chat.SendTo("reci1", Convert.ToBase64String(encrypted1));
                //chat.SendTo("reci1", "encrypted");

            } 

            if (R.Name == "reci2")
            {
                reci2pubkeyblob = Convert.FromBase64String(R.ReceivedData);
                Changetextboxcontents(R.Name + " key ::::::::" + R.ReceivedData +"\r\n");
                //Changetextboxcontents("Alice public key:::::::"+ Convert.ToBase64String(alicepublic) + "\r\n" );
                chat.SendTo("reci2", Convert.ToBase64String(alicepublic));
                encrypted2 = AliceSendsData(textBox2.Text,reci2pubkeyblob);
                Changetextboxcontents("encrypted data2:" + Convert.ToBase64String(encrypted2) + "\r\n");
                chat.SendTo("reci2", Convert.ToBase64String(encrypted2));
            }
            if (R.Name == "reci3")
            {
                reci3pubkeyblob = Convert.FromBase64String(R.ReceivedData);
                Changetextboxcontents(R.Name + " key ::::::::" + R.ReceivedData + "\r\n");
                //Changetextboxcontents("Alice public key:::::::"+ Convert.ToBase64String(alicepublic) + "\r\n" );
                chat.SendTo("reci3", Convert.ToBase64String(alicepublic));
                encrypted3 = AliceSendsData(textBox2.Text, reci3pubkeyblob);
                Changetextboxcontents("encrypted data3:" + Convert.ToBase64String(encrypted3) + "\r\n");
                chat.SendTo("reci3", Convert.ToBase64String(encrypted3));
            }
            
            
        }

        void chat_OnClientDisconnected(object Sender, DisconnectedArguments R)
        {
           // chat.BroadCast(R.Name + " has Disconnected");
            Changetextboxcontents(R.Name + " has disconnected at " + DateTime.Now.ToShortTimeString());
        }

        void chat_OnClientConnected(object Sender, ConnectedArguments R)
        {
            //chat.BroadCast(R.Name + " has Connected");
            Changetextboxcontents(R.Name + " has connected at " + DateTime.Now.ToShortTimeString());
        }

        

       
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            CreateKeys();
            chat = new Server("192.168.2.3", "1234");
            //chat.OnClientConnected+=new OnConnectedDelegate(chat_OnClientConnected);
            chat.OnClientConnected += new OnConnectedDelegate(chat_OnClientConnected);
            chat.OnClientDisconnected += new OnDisconnectedDelegate(chat_OnClientDisconnected);
            chat.OnDataReceived += new OnReceivedDelegate(chat_OnDataReceived);
            chat.OnServerError += new OnErrorDelegate(chat_OnServerError);
            chat.Start();
        }

        

        private void button3_Click(object sender, EventArgs e)
        {
            FileInfo toUpload = new FileInfo(destfilename);

            //Get a new FtpWebRequest object.
            FtpWebRequest request =
                (FtpWebRequest)WebRequest.Create("ftp://"+
                this.textBox4.Text + "/" + toUpload.Name
                );

            //Method will be UploadFile.
            request.Method = WebRequestMethods.Ftp.UploadFile;

            //Set our credentials.
            request.Credentials =
                new NetworkCredential(this.textBox5.Text,
                                        this.textBox6.Text);

            //Setup a stream for the request and a stream for
            // the file we'll be uploading.
            Stream ftpStream = request.GetRequestStream();
            if (ftpStream.CanRead)
            {
                Ftpeventtext("Connected to Ftp server");
            }
            FileStream file = File.OpenRead(destfilename);

            //Setup variables we'll use to read the file.
            int length = 1024;
            byte[] buffer = new byte[length];
            int bytesRead = 0;

            //Write the file to the request stream.
            Ftpeventtext("Writing file to Ftp server");
            do
            {
                bytesRead = file.Read(buffer, 0, length);
                ftpStream.Write(buffer, 0, bytesRead);
            }
            while (bytesRead != 0);

            //Close the streams.
            file.Close();
            ftpStream.Close();

            Ftpeventtext("Upload complete");
        }

        string destfilename = "D://sharedfiles//encrypted.txt";

        private static void EncryptFile(string inputFile, string outputFile, string skey)
        {
            try
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    byte[] key = ASCIIEncoding.UTF8.GetBytes(skey);

                    /* This is for demostrating purposes only. 
                     * Ideally you will want the IV key to be different from your key and you should always generate a new one for each encryption in other to achieve maximum security*/
                    byte[] IV = ASCIIEncoding.UTF8.GetBytes(skey);

                    using (FileStream fsCrypt = new FileStream(outputFile, FileMode.Create))
                    {
                        using (ICryptoTransform encryptor = aes.CreateEncryptor(key, IV))
                        {
                            using (CryptoStream cs = new CryptoStream(fsCrypt, encryptor, CryptoStreamMode.Write))
                            {
                                using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                                {
                                    int data;
                                    while ((data = fsIn.ReadByte()) != -1)
                                    {
                                        cs.WriteByte((byte)data);
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // failed to encrypt file
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

            EncryptFile(textBox7.Text, destfilename, "1234512345678976");
            Ftpeventtext("encryption successful");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog2.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.textBox7.Text = openFileDialog2.FileName;
            }
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
        
    }
}
