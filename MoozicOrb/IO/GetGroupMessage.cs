using System.Data;
using MySql.Data.MySqlClient;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetGroupMessages
    {
        public GetGroupMessages()
        {
            // Default constructor
        }

        public object[] GetMessagesByGroupId(long group_id)
        {
            string queryString = $"SELECT * FROM messages WHERE group_id = {group_id}";
            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return GetObj(dt); 
        }

        public void GetMyMessages(int userID, int senderID)
        {
            string queryString = $"SELECT * FROM messages WHERE senderID = {senderID} and receiverID = {userID}";
            Query query = new Query();
            DataTable dt = query.Run(queryString);

            
        }

        public object[] GetObj(DataTable dt)
        {
            int size = dt.Rows.Count;
            int it = 0;
            GroupMessage[] messageArray = new GroupMessage[size];

            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    messageArray[it] = new GroupMessage();

                    messageArray[it].MessageId = long.Parse(row["message_id"].ToString());
                    messageArray[it].GroupId = long.Parse(row["group_id"].ToString());
                    messageArray[it].SenderId = int.Parse(row["sender_id"].ToString());
                    messageArray[it].MessageText = row["message_text"].ToString();
                    messageArray[it].MessageDeleted = bool.Parse(row["is_deleted"].ToString());
                    
                    messageArray[it].Timestamp = (DateTime)row["DateTime"];

                    it++;
                }

                return messageArray;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
