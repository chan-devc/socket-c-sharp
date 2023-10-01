using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using socket_c_sharp;
using System.Data;
using System.Text.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        // listener.Prefixes.Add("http://*:90/");
        listener.Prefixes.Add("http://192.168.248.149:8080/");
        listener.Start();
        Console.WriteLine("WebSocket server listening");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
        WebSocket webSocket = webSocketContext.WebSocket;

        byte[] buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                JsonDocument jsonDoc = JsonDocument.Parse(message);
                string Name = "";
                string Value = "";

                // access properties
                JsonElement root = jsonDoc.RootElement;

                if (root.TryGetProperty("Name", out JsonElement name))
                {
                    Name = name.GetString();
                }

                if (Name == "Capture")
                {
                    Value = await getImage(message);
                }
                else
                {
                    Value = getLicenseNo(message);
                }

                // Convert the modified message to bytes
                byte[] modifiedMessageBytes = Encoding.UTF8.GetBytes(Value);


                // Echo the received message back to the client
                // await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, true, CancellationToken.None);
                await webSocket.SendAsync(new ArraySegment<byte>(modifiedMessageBytes, 0, modifiedMessageBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }
    }

    // capture the image
    static async Task<string> getImage(string message)
    {
        string Image = "";
        string cameraIp = "";
        string username = ""; // Replace with your camera's username
        string password = ""; // Replace with your camera's IP address or hostname

        JsonDocument jsonDoc = JsonDocument.Parse(message);

        // access properties
        JsonElement root = jsonDoc.RootElement;

        if (root.TryGetProperty("cameraIp", out JsonElement camera))
        {
            cameraIp = camera.GetString();
        }
        if (root.TryGetProperty("username", out JsonElement name))
        {
            username = name.GetString();
        }
        if (root.TryGetProperty("password", out JsonElement pwd))
        {
            password = pwd.GetString();
        }

        string captureImageUrl = $"http://{cameraIp}/ISAPI/Streaming/channels/1/picture"; // Adjust the URL as per your camera's documentation


        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Set up basic authentication headers
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                // Send an HTTP GET request to capture the image
                HttpResponseMessage response = await client.GetAsync(captureImageUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Read and store the image data
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    string base64Image = Convert.ToBase64String(imageBytes);

                    // File.WriteAllBytes("captured_image.jpg", imageBytes);
                    var dataList = new List<Dictionary<string, object>>();
                    var data = new Dictionary<string, object>
                     {
                        {"base64", base64Image}
                     };
                    dataList.Add(data);
                    Image = JsonConvert.SerializeObject(dataList);
                }
                else
                {
                    Console.WriteLine($"HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        return Image;
    }
    // get Vehicles
    static string getLicenseNo(string value)
    {
        string jsonData = "";
        using (var dbConnecttion = new DataBaseConnection())
        {
            JsonDocument jsonDoc = JsonDocument.Parse(value);
            string LicenseNo = "";
            string nDate = "";

            // access properties
            JsonElement root = jsonDoc.RootElement;

            if (root.TryGetProperty("licenseNo", out JsonElement licens))
            {
                LicenseNo = licens.GetString();
            }

            if (root.TryGetProperty("DateTime", out JsonElement dateTime))
            {
                nDate = dateTime.GetString();
            }

            try
            {
                string query = $"select TOP 1 * from TableCarInfo where Car___No like '%{LicenseNo}' and AcceptNo like '%{nDate}%' Order by AcceptNo DESC";
                DataTable result = dbConnecttion.ExecuteQuery(query);

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow item in result.Rows)
                {
                    var Inspection = getInspection($"{item["AcceptNo"]}");
                    var Lamp = getLamp($"{item["AcceptNo"]}");
                    var Smoke = getSmoke($"{item["AcceptNo"]}");
                    var COHC = getCOHC($"{item["AcceptNo"]}");

                    var data = new Dictionary<string, object>
                            {
                              {"AcceptNo", item["AcceptNo"]},
                              {"LicenseNo", item["Car___No"]},
                              {"Axle", item["Tst_Axle"]},
                              {"Inspection", Inspection },
                              {"Lamp", Lamp },
                              {"Smoke", Smoke },
                              {"COHC", COHC },

                            };
                    dataList.Add(data);
                }

                dbConnecttion.Close();

                jsonData = JsonConvert.SerializeObject(dataList);

            }
            catch (System.Exception)
            {

                throw;
            }

        }
        return jsonData;
    }

    //  get inspection
    static string getInspection(string acceptNo)
    {
        string jsonData = "";
        using (var dbConnecttion = new DataBaseConnection())
        {
            try
            {
                string query = $"select * from TableCarData where AcceptNo = '{acceptNo}'";
                DataTable result = dbConnecttion.ExecuteQuery(query);

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow item in result.Rows)
                {
                    var data = new Dictionary<string, object>
                            {
                            {"AcceptNo", item["AcceptNo"]},
                            {"Start_Date", item["ABS__Stt"]},
                            {"End_Date", item["ABS__End"]},
                            {"Side_slip_F", item["SlipV_FF"]},
                            {"Side_slip_FS", item["SlipFFPK"]},
                            {"Side_slip_R", item["SlipV_RR"]},
                            {"Side_slip_RS", item["SlipRRPK"]},
                            {"Side_slip_Sum", item["Slip__PK"]},

                            //   axle one
                            {"Brake_weight_one", item["Axis__FF"]},
                            {"Brake_force_left_axle_one", item["Brk_L_FF"]},
                            {"Brake_force_right_axle_one", item["Brk_R_FF"]},
                            {"Brake_force_sum_axle_one", item["BSum__FF"]},
                            {"Brake_force_diff_axle_one", item["BDiff_FF"]},
                            {"Brake_force_result_axle_one", item["BSumFFPK"]},

                            //   axle two
                            {"Brake_weight_two", item["Axis__RR"]},
                            {"Brake_force_left_axle_two", item["Brk_L_RR"]},
                            {"Brake_force_right_axle_two", item["Brk_R_RR"]},
                            {"Brake_force_sum_axle_two", item["BSum__RR"]},
                            {"Brake_force_diff_axle_two", item["BDiff_RR"]},
                            {"Brake_force_result_axle_two", item["BSumRRPK"]},

                            // hand brake
                            {"Brake_force_left_hand", item["Brk_L_PB"]},
                            {"Brake_force_right_hand", item["Brk_R_PB"]},
                            {"Brake_force_sum_hand", item["BSum__PB"]},
                            {"Brake_force_diff_hand", item["BDiff_PB"]},
                            {"Brake_force_result_hand", item["BSumPBPK"]},

                            //   diff of brake left and right
                            {"Brake_total_left", item["Brk_TotL"]},
                            {"Brake_total_right", item["Brk_TotR"]},
                            {"Brake_total", item["BSumP_PK"]},
                            {"Brake_result", item["Brake_PK"]},

                            
                            // speedemoter
                            {"speedemotor40", item["Speed_40"]},
                            {"speedemotor40_resutl", item["Speed_PK"]},

                            // speedemoter
                            {"weight", item["Axis_SUM"]},


                            };
                    dataList.Add(data);
                }
                jsonData = JsonConvert.SerializeObject(dataList);
                dbConnecttion.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        return jsonData;
    }

    //  get lamp
    static string getLamp(string acceptNo)
    {
        string jsonData = "";
        using (var dbConnecttion = new DataBaseConnection())
        {
            try
            {
                string query = $"select AcceptNo,HTstTime,LLuc0_UD,LLuc0_LR,RLuc0Val,RLuc0_PK from TableCar_HLT where AcceptNo = '{acceptNo}'";
                DataTable result = dbConnecttion.ExecuteQuery(query);

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow item in result.Rows)
                {
                    var data = new Dictionary<string, object>
                            {
                            {"AcceptNo", item["AcceptNo"]},
                            {"DateIn", item["HTstTime"]},
                            {"light_light", item["LLuc0_UD"]},
                            {"light_low", item["LLuc0_LR"]},
                            {"lamp_opactity", item["RLuc0Val"]},
                            {"lamp_result", item["RLuc0_PK"]},
                            };
                    dataList.Add(data);
                }
                jsonData = JsonConvert.SerializeObject(dataList);
                dbConnecttion.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        return jsonData;
    }

    //  get smoke
    static string getSmoke(string acceptNo)
    {
        string jsonData = "";
        using (var dbConnecttion = new DataBaseConnection())
        {
            try
            {
                string query = $"select AcceptNo,Opa_Date,OpaAverage,Opacity_PK from TableCar_Opa where AcceptNo = '{acceptNo}'";
                DataTable result = dbConnecttion.ExecuteQuery(query);

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow item in result.Rows)
                {
                    var data = new Dictionary<string, object>
                            {
                            {"AcceptNo", item["AcceptNo"]},
                            {"DateIn", item["Opa_Date"]},
                            {"smoke_average", item["OpaAverage"]},
                            {"smoke_result", item["Opacity_PK"]}
                            };
                    dataList.Add(data);
                }
                jsonData = JsonConvert.SerializeObject(dataList);
                dbConnecttion.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        return jsonData;
    }

    //  get COHC
    static string getCOHC(string acceptNo)
    {
        string jsonData = "";
        using (var dbConnecttion = new DataBaseConnection())
        {
            try
            {
                string query = $"select AcceptNo,CoHcDate,TstL__CO,TstL__HC,Smoke_PK from TableCarCOHC where AcceptNo = '{acceptNo}'";
                DataTable result = dbConnecttion.ExecuteQuery(query);

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow item in result.Rows)
                {
                    var data = new Dictionary<string, object>
                            {
                            {"AcceptNo", item["AcceptNo"]},
                            {"DateIn", item["CoHcDate"]},
                            {"CO", item["TstL__CO"]},
                            {"HC", item["TstL__HC"]},
                            {"HC", item["Smoke_PK"]},
                            };
                    dataList.Add(data);
                }
                jsonData = JsonConvert.SerializeObject(dataList);
                dbConnecttion.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        return jsonData;
    }
}
