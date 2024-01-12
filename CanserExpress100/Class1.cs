using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CanserExpress100
{
    [Description("取消快递"), HotUpdate]
    public class CanserExpress100 : AbstractListPlugIn
    {
        internal static MD5 md5 = new MD5CryptoServiceProvider();
        static long appid = 1683505329;
        static string appKey = "63509e262210f73a0452ce7b15442a1e";
        static string business_id = "11490113630";//测试企业id1161885087
        string url = "https://openic.sf-express.com/open/api/external/cancelorder?sign=";
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase("test"))
            {
                Log.Write("取消顺丰0");
                if (this.ListView.SelectedRowsInfo == null || this.ListView.SelectedRowsInfo.Count == 0)
                {
                    this.View.ShowMessage("没有选择任何数据，请先选择数据！");
                    return;
                }
                Log.Write("取消顺丰1");
                IListView lv = this.View as IListView;
                ListSelectedRowCollection selectedRows = lv.SelectedRowsInfo;
                foreach(ListSelectedRow row in selectedRows) {
                    IDataRow rowdata = row.DataRow as IDataRow;
                    string kdnumber = rowdata["FKuaidiNum"].ToString();
                    long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    JObject postData = new JObject();
                    postData.Add("dev_id", appid);//同城开发者ID
                    postData.Add("push_time", timestamp);//推单时间
                    //postData.Add("order_id", rowdata["FKuaidiNum"].ToString());//快递单号
                    //postData.Add("dev_id", 1679240590);//同城开发者ID
                    postData.Add("order_id", "JS8754946071832");//快递单号
                    
                    String sign = generateOpenSign(postData.ToString(), appid, appKey);
                    //String sign = generateOpenSign(postData.ToString(), 1679240590, "0b99a253019afa0a953a03a3c12af03a");

                    Log.Write("url+sign：" + url + sign + "----postData:" + postData.ToString());

                    string backResult = Post(url + sign, postData.ToString());
                    Log.Write("取消顺丰JSON:"+ postData.ToString());
                    JObject jsonObject = JObject.Parse(backResult);
                    if ((int)jsonObject["error_code"] == 0)//0是取消成功
                    {
                        string strsql = string.Format(@"/*dialect*/ UPDATE T_SAL_DELIVERYNOTICETRACE
                        SET FCARRYBILLNOTYPE='{0}' WHERE FCARRYBILLNO = '{1}';"
                        , "A", kdnumber);//A取消单

                        Log.Write("取消顺丰单成功" );
                    }
                }
            }
        }







        public static String generateOpenSign(String postData, long appId, String appKey)
        {
            /*StringBuilder sb = new StringBuilder();
            sb.Append(postData);
            sb.Append("&" + appId + "&" + appKey);
            Log.Write("需要md5加密数据：" + sb.ToString());*/
            string data = string.Format(@"{0}&{1}&{2}", postData, appId, appKey);
            string ret = DoMD5v2(data.ToString());
            return ret;
        }
        public static string DoMD5v2(string prestr)
        {

            byte[] md5Bytes = md5.ComputeHash(Encoding.GetEncoding("utf-8").GetBytes(prestr));
            StringBuilder sbHex = new StringBuilder();
            foreach (byte b in md5Bytes)
            {
                sbHex.Append(b.ToString("x2"));
            }

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(sbHex.ToString());
            string base64Encoded = Convert.ToBase64String(utf8Bytes);

            return base64Encoded;
        }
        public static string Post(string Url, string jsonParas)
        {
            string strURL = Url;
            // 创建一个 HTTP 请求
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
            // 设置请求方法为 POST
            request.Method = "POST";
            // 设置内容类型为 application/json
            request.ContentType = "application/json";

            // 设置请求头的 Auth 字段request.Headers.Add("Auth", auth);


            // 将请求参数转换为字节数组
            byte[] payload = Encoding.UTF8.GetBytes(jsonParas);

            // 设置请求的 ContentLength
            request.ContentLength = payload.Length;

            // 发送请求，获取请求流
            Stream writer;
            try
            {
                writer = request.GetRequestStream();
            }
            catch (Exception)
            {
                writer = null;
                Console.Write("连接服务器失败!");
            }

            // 将请求参数写入流
            writer.Write(payload, 0, payload.Length);
            writer.Close(); // 关闭请求流

            HttpWebResponse response;
            try
            {
                // 获取响应流
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
            }

            Stream s = response.GetResponseStream();
            StreamReader sRead = new StreamReader(s);
            string postContent = sRead.ReadToEnd();
            sRead.Close();
            Log.Write($"取消快递服务端返回信息：{postContent}, 时间：{DateTime.Now.ToString()}");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"服务端返回信息：{postContent}, 时间：{DateTime.Now.ToString()}");

            return postContent; // 返回 JSON 数据
        }
    }
}
