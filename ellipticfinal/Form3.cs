using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using NetworksApi.TCP.CLIENT;
using NetworksApi.DATA.ENCRYPTION;


namespace ellipticfinal
{
    public partial class Form3 : Form
    {
        static CngKey recipient1;


        static byte[] alicePubKeyBlob;
        static byte[] reci1pubkeyblob;
        static byte[] encrypted;

        //static string encrypted;

        private static void CreateKeys()
        {

            recipient1 = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);

            reci1pubkeyblob = recipient1.Export(CngKeyBlobFormat.EccPublicBlob);
        }

        private byte[] BobReceivesData(byte[] encryptedData)
        {
            //Console.WriteLine("Bob receives encrypted data");
            byte[] rawData = null;

            var aes = new AesCryptoServiceProvider();

            int nBytes = aes.BlockSize >> 3;
            byte[] iv = new byte[nBytes];
            for (int i = 0; i < iv.Length; i++)
                iv[i] = encryptedData[i];

            using (var bobAlgorithm = new ECDiffieHellmanCng(recipient1))
            using (CngKey alicePubKey = CngKey.Import(alicePubKeyBlob,
                  CngKeyBlobFormat.EccPublicBlob))
            {
                byte[] symmKey = bobAlgorithm.DeriveKeyMaterial(alicePubKey);

                Changetextboxcontents("symmkey::::"+Convert.ToBase64String(symmKey));
                //Console.WriteLine("Bob creates this symmetric key with " +
                //    "Alices public key information: {0}",
                //  Convert.ToBase64String(symmKey));

                aes.Key = symmKey;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream ms = new MemoryStream())
                {
                    var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write);
                    cs.Write(encryptedData, nBytes, encryptedData.Length - nBytes);
                    cs.Close();

                    rawData = ms.ToArray();

                    // Console.WriteLine("Bob decrypts message to: {0}",
                    //Encoding.UTF8.GetString(rawData));
                }
                aes.Clear();
            }
            return rawData;
        }

        Client clientside;
        public delegate void updatetext(string txt);
        public Form3()
        {
            InitializeComponent();
        }
        void Changetextboxcontents(string txt)
        {
            if (textBox3.InvokeRequired)
            {
                Invoke(new updatetext(Changetextboxcontents), new object[] { txt });

            }
            else
            {
                textBox3.Text += txt + "\r\n";
            }
        }
        void ftpservercontents(string txt)
        {
            if (textBox3.InvokeRequired)
            {
                Invoke(new updatetext(ftpservercontents), new object[] { txt });

            }
            else
            {
                textBox10.Text += txt + "\r\n";
            }
        }
        
        int counter = 0;
        void clientside_OnDataReceived(object Sender, ClientReceivedArguments R)
        {
            counter += 1;
            if (counter == 1)
            {
                alicePubKeyBlob = Convert.FromBase64String(R.ReceivedData);
                Changetextboxcontents("Alicepublic:::::" + R.ReceivedData + "\r\n");
            }
            //Changetextboxcontents("check::::"+Convert.ToBase64String(alicePubKeyBlob));
            if (counter == 2)
            {
                Changetextboxcontents("encrypted data:::::" + R.ReceivedData +"\r\n");
                encrypted = BobReceivesData(Convert.FromBase64String(R.ReceivedData));
                Changetextboxcontents("data:::" + Encoding.UTF8.GetString(encrypted));

            }
        }

        void clientside_OnClientError(object Sender, ClientErrorArguments R)
        {
            MessageBox.Show(R.ErrorMessage);
        }

        void clientside_OnClientDisconnected(object Sender, ClientDisconnectedArguments R)
        {
            Changetextboxcontents(R.EventMessage);
        }

        void clientside_OnClientConnecting(object Sender, ClientConnectingArguments R)
        {
            Changetextboxcontents(R.EventMessage);   
        }

        void clientside_OnClientConnected(object Sender, ClientConnectedArguments R)
        {
            Changetextboxcontents(R.EventMessage);
        }

       

        private void button1_Click(object sender, EventArgs e)
        {
            clientside.ServerIp = textBox1.Text;
            clientside.ServerPort = "1234";
            clientside.ClientName = textBox2.Text;
            clientside.Connect();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CreateKeys();
            if (clientside.IsConnected)
            {
                Changetextboxcontents("reci1key::::::" + Convert.ToBase64String(reci1pubkeyblob) + "\r\n");
                clientside.Send(Convert.ToBase64String(reci1pubkeyblob));
            }
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            clientside = new Client();
            clientside.OnClientConnected += new OnClientConnectedDelegate(clientside_OnClientConnected);
            clientside.OnClientConnecting += new OnClientConnectingDelegate(clientside_OnClientConnecting);
            clientside.OnClientDisconnected += new OnClientDisconnectedDelegate(clientside_OnClientDisconnected);
            clientside.OnClientError += new OnClientErrorDelegate(clientside_OnClientError);
            clientside.OnDataReceived += new OnClientReceivedDelegate(clientside_OnDataReceived);
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }


        private void button3_Click(object sender, EventArgs e)
        {
            WebClient request = new WebClient();

            //Setup our credentials
            request.Credentials =
                new NetworkCredential(this.textBox6.Text,
                                        this.textBox7.Text);

            //Download the data into a Byte array
            byte[] fileData =
                request.DownloadData(this.textBox5.Text + "/" +
                                     this.textBox8.Text);

            //Create a FileStream that we'll write the
            // byte array to.
            FileStream file =
                File.Create(this.textBox9.Text + "\\" +
                this.textBox8.Text);

            //Write the full byte array to the file.
            file.Write(fileData, 0, fileData.Length);

            //Close the file so other processes can access it.
            //file.Close();
            
            MessageBox.Show("Download complete");
            DecryptFile(file, "D://decrypt.jpg", "1234512345678976");
            MessageBox.Show("file decrypted and stored to d://decrypt.jpg");
             file.Close();
        }


        private static void DecryptFile(FileStream inputFile, string outputFile, string skey)
        {
            try
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    byte[] key = ASCIIEncoding.UTF8.GetBytes(skey);

                    /* This is for demostrating purposes only. 
                     * Ideally you will want the IV key to be different from your key and you should always generate a new one for each encryption in other to achieve maximum security*/
                    byte[] IV = ASCIIEncoding.UTF8.GetBytes(skey);

                    using (FileStream fsCrypt = inputFile)
                    {
                        using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                        {
                            using (ICryptoTransform decryptor = aes.CreateDecryptor(key, IV))
                            {
                                using (CryptoStream cs = new CryptoStream(fsCrypt, decryptor, CryptoStreamMode.Read))
                                {
                                    int data;
                                    while ((data = cs.ReadByte()) != -1)
                                    {
                                        fsOut.WriteByte((byte)data);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // failed to decrypt file
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FtpWebRequest request =
                (FtpWebRequest)WebRequest.Create(this.textBox5.Text + "/" );
            

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials =
                new NetworkCredential(this.textBox6.Text,
                                        this.textBox7.Text);

            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            ftpservercontents(reader.ReadToEnd());

            ftpservercontents("Directory List Complete, status {0}"+response.StatusDescription);

            reader.Close();
            response.Close();
        }

        

    }
}
