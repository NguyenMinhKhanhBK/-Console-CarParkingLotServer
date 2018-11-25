using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Quan_Ly_Bai_Xe
{
    class Program
    {
      static void Main(string[] args)
        {
            var s = "{'D2C':'true','BD':1,'FP7Count':2,'FP7Status':'121212121212121212121212','SumAvai':5}";
            writeToBuildingTable(s);
            UpdateCarParkingLayoutTable(1);
        }

        //Process received messages from PLC S7-1500 and save in SQL Building table
       static  void writeToBuildingTable(string s)
        {
            var a = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(s);
            long ID = a["BD"];
            int FP7count = (int) a["FP7Count"];
            string FP7status = a["FP7Status"];
            int sumAvai = (int)a["SumAvai"];
            using (CarParkingLotEntities data = new CarParkingLotEntities())
            {
                var item = data.Buildings.Where(p => p.ID == ID).FirstOrDefault();
                if (item != null)
                {
                    item.FP7Count = FP7count;
                    item.FP7Status = FP7status;
                    item.SumAvail = sumAvai;
                    data.SaveChanges();
                    Console.OutputEncoding = System.Text.Encoding.UTF8;
                    Console.WriteLine("Đã lưu vào CSDL");
                }
                else
                {
                    Console.OutputEncoding = System.Text.Encoding.UTF8;
                    Console.WriteLine("Không tìm thấy thông tin Building");
                }
            }
        }

        //Check the current status of Building table and update to CarParkingLayout table if necessary
        static void UpdateCarParkingLayoutTable(int BuildingID)
        {
            using (CarParkingLotEntities data = new CarParkingLotEntities())
            {
                var BuildingTable = data.Buildings.Where(p => p.ID == BuildingID).FirstOrDefault();
                
                if(BuildingTable != null)
                {
                    for (int i = 1; i <= BuildingTable.FP7Count; i++)   //Block size
                    {
                        var posIndex = 1;
                        var blockStatus = SplitBlock(BuildingTable.FP7Status, 12).ToList();
                        foreach (var item in blockStatus[i-1])
                        {
                            int currentStatus=0;
                            if (item == '1') currentStatus = 1;
                            else if (item == '2') currentStatus = 2;
                            else if (item == '3') currentStatus = 3;
                            else if (item == '4') currentStatus = 4;

                            var layoutTable = data.CarParkingLayouts.Where(p => p.BuildingID == BuildingID && p.BlockID == i && p.PositionID == posIndex).FirstOrDefault();
                            if (currentStatus >= 1 && currentStatus <= 4)
                            {
                                if(currentStatus != layoutTable.StatusID)
                                {
                                    if (currentStatus!=1)
                                    {
                                        layoutTable.UserID = null;
                                        layoutTable.LicensePlate = null;
                                        layoutTable.ReservedTime = null;
                                        layoutTable.ArrivalTime = null;
                                    }
                                }
                                layoutTable.StatusID = currentStatus;
                            }
                            data.SaveChanges();
                            posIndex++;
                            
                        }
                    }
                }
                
            }
        }

        //Divide a string to multiple blocks of chunkSize
        static IEnumerable<string> SplitBlock(string str, int chunkSize)
        {
            if (str == null || chunkSize == 0 ) return null;
            else return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }



    }



    public class S71500Message
    {
        string D2C;
        string BuildingID;
        string FP7Count;
        string FP7Status;
        string SumAvail;
    }




}
