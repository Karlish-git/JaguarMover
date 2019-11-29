using System;

namespace JaguarMover
{
    public class JagController
    {
        private JaguarComm comm;

        public bool ProtectMotorTemp;
//        private MainWindow _mainWindow;


        #region ReciveVars

        private const string SensorPackageId = "#";
        private const string GpsPackageId = "$GPRMC";

        private const string MotorPackageId = "MM";
        //        private delegate void UpdateMainUiDelegate(string msg);

        //for temperature sensor
        private readonly double[] resTable =
        {
            114660, 84510, 62927, 47077, 35563, 27119, 20860, 16204, 12683, 10000,
            7942, 6327, 5074, 4103, 3336, 2724, 2237, 1846, 1530, 1275, 1068, 899.3, 760.7, 645.2, 549.4
        };

        private readonly double[] tempTable =
            {-20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100};

        //TODO These sould be made usable to all 
        //        private const string COMM1_ID = "COMM1";
        public MotorData[] MotorData = new MotorData[4];
        private readonly string[] MM_PACKAGE_ID = {"MM0", "MM1", "MM2", "MM3"};

        #endregion


        /// <summary>
        /// change the state of light
        /// </summary>
        /// <param name="state">the state to change to</param>
        public void Light(bool state)
        {
            //int powerIO = 0xff;   // 7th bit is for light
            //            comm.SendCommand("SYS MMC " + (state ? (0xff | 0x80) : (0xff & 0x7f)).ToString());  did not work??
            int powerIO = 0xff;
            if (state)
            {
                powerIO &= 0x7f; //bit 7 turn off lights
            }
            else
            {
                powerIO |= 0x80; //bit 7 turn on lights
            }

            string strCmd = "SYS MMC " + powerIO.ToString();
            comm.SendCommand(strCmd);
        }

        public void Connect()
        {
            if (comm != null)
            {
                comm.Close();
            }

            comm = new JaguarComm(this);
            comm.StartClient("192.168.0.60", 10001);
        }

        public void Disconnect()
        {
            comm.Close();
        }

        /// <summary>
        /// enable the e-breaks
        /// </summary>
        public void Stop()
        {
            comm.SendCommand("MMW !M 0 0");
            comm.SendCommand("MMW !EX");
        }

        /// <summary>
        /// Input Values from -1000 to 1000.
        /// negative to go backward or left.
        /// </summary>
        public void Move(int forvard, int turn)
        {
            int forwardPwm;
            int turnPwm;
            if (!ProtectMotorTemp)
            {
                forwardPwm = -1 * forvard; //TODO: mby change -1 to 0
                turnPwm = -1 * turn;
            }
            else
            {
                forwardPwm = 0;
                turnPwm = 0;
            }

            int cmd1 = forwardPwm + turnPwm;
            int cmd2 = forwardPwm - turnPwm;
            SendMotorCmd(cmd1, cmd2); //, cmd1, cmd2);
        }

        public void StartMove()
        {
            comm.SendCommand("MMW !MG");
        }

        public void MoveFlippers(int front, int back)
        {
            comm.SendCommand("MM2 !PR 2 " + front.ToString());
            comm.SendCommand("MM2 !PR 1 " + (back).ToString());

        }


        private void SendMotorCmd(int cmd1, int cmd2) //, int cmd3, int cmd4)
        {
            cmd1 = (cmd1 < -1000 ? -1000 : cmd1);
            cmd1 = (cmd1 > 1000 ? 1000 : cmd1);

            cmd2 = (cmd2 < -1000 ? -1000 : cmd2);
            cmd2 = (cmd2 > 1000 ? 1000 : cmd2);

            //            cmd3 = (cmd3 < -1000 ? -1000 : cmd3);
            //            cmd3 = (cmd3 > 1000 ? 1000 : cmd3);
            //
            //            cmd4 = (cmd4 < -1000 ? -1000 : cmd4);
            //            cmd4 = (cmd4 > 1000 ? 1000 : cmd4);

            comm.SendCommand("MMW " + "!M " + cmd1.ToString() + " " + (-cmd2).ToString());
        }

        public void SendCommand(string comand)
        {
            comm.SendCommand(comand);
        }


        //===================== Receive Part =====================//

        #region RecivedParser

        public void Parse(string msg)
        {
            //process message here

            //            Msg = Msg.Remove(0, COMM1_ID.Length);

            if (msg.StartsWith("\n"))
                msg = msg.Remove(0, 1);

            if (msg.StartsWith(SensorPackageId))
            {
                // sensor package here, #seqNum,Yaw,_,GYRO,_,_,_,ACC,_,_,_,COMP,_,_,_,PRES,_,
                //                processIMUMsg(Msg);
            }
            else if (msg.StartsWith(GpsPackageId))
            {
                //GPS sensor here
                //                textBoxGPSRCV.Text = Msg;
                //                ProcessGpsMsg(Msg);
            }
            else if (msg.StartsWith(MotorPackageId))
            {
                //motor driver board message here
                ProcessMotorMsg(msg);
            }
            else
            {
                //other system message here
            }
        }

        private void ProcessMotorMsg(string msg)
        {
            //motor driver board sensor package
            //maybe there are 4 :MM0,MM1,MM2,MM3
            for (int i = 0; i < 4; i++)
            {
                if (msg.StartsWith(MM_PACKAGE_ID[i]))
                {
                    msg = msg.Remove(0, MM_PACKAGE_ID[i].Length + 1); // remove motor id and space 
                    if (msg.StartsWith("A=")) // Read Motor Amps
                    {
                        //here is the current message
                        try
                        {
                            msg = msg.Remove(0, 2);
                            string[] strData = msg.Split(':');
                            MotorData[i].motAmp1 = double.Parse(strData[0]) / 10;
                            MotorData[i].motAmp2 = double.Parse(strData[1]) / 10;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("AI=")) //Get Temperature???
                    {
                        //here is the anolog input message,
                        try
                        {
                            msg = msg.Remove(0, 3);
                            string[] strData = msg.Split(':');
                            MotorData[i].ai3 = double.Parse(strData[2]);
                            MotorData[i].ai4 = double.Parse(strData[3]);
                            //use 10K resistor to GND, translate to temperature
                            double res = Trans2Temperature(MotorData[i].ai3);
                            MotorData[i].motTemp1 = res;
                            res = Trans2Temperature(MotorData[i].ai4);
                            MotorData[i].motTemp2 = res;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("C="))
                    {
                        //here is the encoder position message
                        try
                        {
                            msg = msg.Remove(0, 2);
                            string[] strData = msg.Split(':');
                            MotorData[i].motEncP1 = int.Parse(strData[0]);
                            MotorData[i].motEncP2 = int.Parse(strData[1]);

                            //                            if (i == 2)
                            //                            {
                            //                                flipArmMotor[0].encoderPos = motorData[i].motEncP1; //TODO see WTF use 2 vars for one thing 
                            //                                flipArmMotor[1].encoderPos = motorData[i].motEncP2;
                            //                                GetFrontFlipAngle();
                            //                            }
                            //                            else if (i == 3)
                            //                            {
                            //                                flipArmMotor[2].encoderPos = motorData[i].motEncP1;
                            //                                flipArmMotor[3].encoderPos = motorData[i].motEncP2;
                            //                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    #region driverDigitalIO_mbyDeleate

                    else if (msg.StartsWith("D=")) // TODO see what are they used for. mby remove
                    {
                        //here is the digital input message
                        try
                        {
                            msg = msg.Remove(0, 2);
                            byte data = byte.Parse(msg);
                            //                            int test = data & 0x4;    TODO remove if no need
                            MotorData[i].di3 = (data & 0x4) != 0 ? 1 : 0;
                            MotorData[i].di4 = (data & 0x8) != 0 ? 1 : 0;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("DO=")) // TODO see what are they used for. mby remove
                    {
                        //here is the digital output message
                        try
                        {
                            msg = msg.Remove(0, 3);
                            byte data = byte.Parse(msg);
                            MotorData[i].do1 = (data & 0x1) != 0 ? 1 : 0;
                            MotorData[i].do2 = (data & 0x2) != 0 ? 1 : 0;
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    #endregion

                    else if (msg.StartsWith("P=")) //Read Motor Power Output Applied
                    {
                        try
                        {
                            msg = msg.Remove(0, 2);
                            string[] strData = msg.Split(':');
                            MotorData[i].motPower1 = int.Parse(strData[0]);
                            MotorData[i].motPower2 = int.Parse(strData[1]);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("S=")) //Read Encoder Motor Speed in RPM
                    {
                        //here is the velocity message 
                        try
                        {
                            msg = msg.Remove(0, 2);
                            string[] strData = msg.Split(':');
                            MotorData[i].motEncS1 = int.Parse(strData[0]);
                            MotorData[i].motEncS2 = int.Parse(strData[1]);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("T="))
                    {
                        //here is the motor driver board temperature message
                        try
                        {
                            msg = msg.Remove(0, 2);
                            string[] strData = msg.Split(':');
                            MotorData[i].ch1Temp = double.Parse(strData[0]);
                            MotorData[i].ch2Temp = double.Parse(strData[1]);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("V="))
                    {
                        //here is the power voltage message
                        try
                        {
                            msg = msg.Remove(0, 2);
                            string[] strData = msg.Split(':');
                            MotorData[i].drvVoltage = double.Parse(strData[0]) / 10;
                            MotorData[i].batVoltage = double.Parse(strData[1]) / 10;
                            MotorData[i].reg5VVoltage = double.Parse(strData[2]) / 1000;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("CR="))
                    {
                        //here is the encoder relative reading message
                        /*
                        Returns the amount of counts that have been measured from the last time this query was
                        made. Relative counter read is sometimes easier to work with, compared to full counter
                        reading, as smaller numbers are usually returned.*/
                        try
                        {
                            msg = msg.Remove(0, 3);
                            string[] strData = msg.Split(':');
                            MotorData[i].motEncCR1 = int.Parse(strData[0]);
                            MotorData[i].motEncCR2 = int.Parse(strData[1]);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else if (msg.StartsWith("FF="))
                    {
                        //here is the motor driver board message
                        try
                        {
                            msg = msg.Remove(0, 3);
                            byte data = byte.Parse(msg);
                            MotorData[i].statusFlag = data;
                            if ((data & 0x1) != 0)
                            {
                                //checkBoxOverHeat.Checked = true;
                                MotorData[i].erorMsg = "OH";
                            }
                            else
                            {
                                //checkBoxOverHeat.Checked = false;
                            }

                            if ((data & 0x2) != 0)
                            {
                                //checkBoxOverVol.Checked = true;
                                MotorData[i].erorMsg += "+OV";
                            }
                            else
                            {
                                //checkBoxOverVol.Checked = false;
                            }

                            if ((data & 0x4) != 0)
                            {
                                //checkBoxUnderVol.Checked = true;
                                MotorData[i].erorMsg += "+UV";
                            }
                            else
                            {
                                //checkBoxUnderVol.Checked = false;
                            }

                            if ((data & 0x8) != 0)
                            {
                                //checkBoxShort.Checked = true;
                                MotorData[i].erorMsg += "SHT";
                            }
                            else
                            {
                                //checkBoxShort.Checked = false;
                            }

                            if ((data & 0x10) != 0) // TODO add Estop data
                            {
                                //checkBoxEStop.Checked = true;
                                // ReSharper disable once StringLiteralTypo
                                MotorData[i].erorMsg += "+ESTOP";
                                if ((i == 0) || (i == 1))
                                {
                                    // blnEStop = true;
                                }
                                else if (i == 2)
                                {
                                    //frontFlipEStop = true;
                                }
                                else if (i == 3)
                                {
                                    //rearFlipEStop = true;
                                }
                            }
                            else
                            {
                                //checkBoxEStop.Checked = false;
                                if ((i == 0) || (i == 1))
                                {
                                    //blnEStop = false;
                                }
                                else if (i == 2)
                                {
                                    //frontFlipEStop = false;
                                }
                                else if (i == 3)
                                {
                                    //rearFlipEStop = false;
                                }
                            }

                            if ((data & 0x20) != 0)
                            {
                                //checkBoxSepexFault.Checked = true;
                                MotorData[i].erorMsg += "SEPF";
                            }
                            else
                            {
                                //checkBoxSepexFault.Checked = false;
                            }

                            if ((data & 0x40) != 0)
                            {
                                //checkBoxPromFault.Checked = true;
                                MotorData[i].erorMsg += "+PromF";
                            }
                            else
                            {
                                //checkBoxPromFault.Checked = false;
                            }

                            if ((data & 0x80) != 0)
                            {
                                //checkBoxConfigFault.Checked = true;
                                MotorData[i].erorMsg += "+ConfF";
                            }
                            else
                            {
                                //checkBoxConfigFault.Checked = false;
                            }

                            //                            if (motorData[i].erorMsg.Length > 1)
                            //                            {
                            //                                if (i == 0)
                            //                                    lblMD1State.Text = motorData[i].erorMsg;
                            //                                else if (i == 1)
                            //                                    lblMD2State.Text = motorData[i].erorMsg;
                            //                                else if (i == 2)
                            //                                    lblMD3State.Text = motorData[i].erorMsg;
                            //                                else if (i == 3)
                            //                                    lblMD4State.Text = motorData[i].erorMsg;
                            //                            }
                            //                            else
                            //                            {
                            //                                if (i == 0)
                            //                                    lblMD1State.Text = "OK";
                            //                                else if (i == 1)
                            //                                    lblMD2State.Text = "OK";
                            //                                else if (i == 2)
                            //                                    lblMD3State.Text = "OK";
                            //                                else if (i == 3)
                            //                                    lblMD4State.Text = "OK";
                            //                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }

        private double Trans2Temperature(double adValue)
        {
            //for new temperature sensor
            double tempM;
            double k = (adValue / 4095); // was Fullad
            double resValue;
            if (Math.Abs(k) > 1)
            {
                resValue = 10000 * (1 - k) / k; //AD value to resistor
            }
            else
            {
                resValue = resTable[0];
            }

            int index = -1;
            if (resValue >= resTable[0]) //too lower
            {
                tempM = -20;
            }
            else if (resValue <= resTable[24])
            {
                tempM = 100;
            }
            else
            {
                for (int i = 0; i < 24; i++)
                {
                    if ((resValue <= resTable[i]) && (resValue >= resTable[i + 1]))
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    tempM = tempTable[index] + (resValue - resTable[index]) / (resTable[index + 1] - resTable[index]) *
                            (tempTable[index + 1] - tempTable[index]);
                }
                else
                {
                    tempM = 0;
                }
            }

            return tempM;
        }


        //TODO add GPS and IMU data

        #region IMU&GPS__Data  

        //        private void processIMUMsg(string msg)
        //        {
        //            msg = msg.Remove(0, 1);
        //
        //            string[] data = msg.Split(',');
        //
        //            seqNo = int.Parse(data[0]);
        //
        //            accel_x = double.Parse(data[8]);
        //            accel_y = double.Parse(data[9]);
        //            accel_z = double.Parse(data[10]);
        //            gyro_x = double.Parse(data[4]);
        //            gyro_y = double.Parse(data[5]);
        //            gyro_z = double.Parse(data[6]);
        //
        //            //this sYaw value only for short time estimation, used it for sensor occ_map rotate 
        //            sYaw = double.Parse(data[2]);
        //
        //            if (sYaw < -Math.PI) sYaw = 2 * Math.PI + sYaw;
        //            if (sYaw > Math.PI) sYaw = -(2 * Math.PI - sYaw);
        //            // lblEstimateYaw.Text = (sYaw / Math.PI * 180).ToString("0.00");
        //
        //            imuRecord.accel_x = accel_x;
        //
        //            imuRecord.accel_y = accel_y;
        //
        //            imuRecord.accel_z = accel_z;
        //
        //            imuRecord.gyro_x = gyro_x;
        //
        //            imuRecord.gyro_y = gyro_y;
        //
        //            imuRecord.gyro_z = gyro_z;
        //
        //            imuRecord.index = seqNo;
        //
        //            imuRecord.eYaw = sYaw;
        //
        //            //for drawing
        //            draw_AccelX[drawEndPoint] = accel_x;
        //            draw_AccelY[drawEndPoint] = accel_y;
        //            draw_AccelZ[drawEndPoint] = accel_z;
        //            draw_GyroX[drawEndPoint] = gyro_x;
        //            draw_GyroY[drawEndPoint] = gyro_y;
        //            draw_GyroZ[drawEndPoint] = gyro_z;
        //            drawEndPoint++;
        //            if (drawEndPoint >= DrawDataLen)
        //            {
        //                drawEndPoint = 0;
        //
        //            }
        //
        //            if (drawEndPoint == drawStartPoint)
        //            {
        //                drawStartPoint++;
        //                if (drawStartPoint >= DrawDataLen)
        //                    drawStartPoint = 0;
        //            }
        //            if (startRecord)
        //            {
        //                String recTemp = imuRecord.index.ToString() + "," +
        //                                imuRecord.accel_x.ToString() + "," +
        //                                imuRecord.accel_y.ToString() + "," +
        //                                imuRecord.accel_z.ToString() + "," +
        //                                imuRecord.gyro_x.ToString() + "," +
        //
        //                                imuRecord.gyro_y.ToString() + "," +
        //
        //                                imuRecord.gyro_z.ToString() + "," +
        //                                imuRecord.eYaw.ToString();
        //
        //
        //                SW.WriteLine(recTemp);
        //                recordCnt++;
        //                if (recordCnt > MAXFILELEN)
        //                {
        //                    recordCnt = 0;
        //                    SW.Close();
        //                    //open next file
        //
        //                    SW = File.CreateText(fileName + fileCnt.ToString() + ".txt");
        //                    fileCnt++;
        //
        //                }
        //            }
        //
        //        }
        //
        //        private void ProcessGpsMsg(string msg)
        //        {
        //
        //            string[] data = msg.Split(',');
        //            double dirLat = 1;
        //            double dirLongitude = -1;
        //            if ((data[0] == "$GPRMC") && (data[3] != "") && (data[5] != ""))
        //            {
        //                if (startRecord)
        //                {
        //                    SW.WriteLine(gpsRecord.recRaw);
        //                    recordCnt++;
        //                    if (recordCnt > MAXFILELEN)
        //                    {
        //                        recordCnt = 0;
        //                        SW.Close();
        //                        //open next file
        //
        //                        SW = File.CreateText(fileName + fileCnt.ToString() + ".txt");
        //                        fileCnt++;
        //
        //                    }
        //                }
        //
        //                gpsRecord.timeStamp = double.Parse(data[1]);     //time
        //                gpsRecord.status = data[2];                        //A- valid, V = invalid
        //                if (gpsRecord.status == "A")
        //                {
        //                    gpsRecord.qi = 1;
        //                }
        //                else
        //                {
        //                    gpsRecord.qi = 0;
        //                }
        //
        //                gpsRecord.latitude = double.Parse(data[3]);
        //                gpsRecord.latHemi = data[4];
        //
        //                gpsRecord.longitude = double.Parse(data[5]);   //dddmm.mmmm
        //                gpsRecord.lngHemi = data[6];
        //
        //                //DCM
        //                if ((data[7] != "") && data[8] != null)
        //                {
        //                    vog = double.Parse(data[7]) * KNNOT2MS; //speed
        //                    cog = double.Parse(data[8]) * D2R; //course over ground
        //                    if (cog > Math.PI) cog = cog - 2 * Math.PI;
        //
        //                    gpsRecord.vog = double.Parse(data[7]) * KNNOT2MS;
        //                    gpsRecord.cog = double.Parse(data[8]);
        //                }
        //
        //                if (gpsRecord.latHemi == "N")
        //                    dirLat = 1;
        //                else
        //                    dirLat = -1;
        //
        //                if (gpsRecord.lngHemi == "W")
        //                    dirLongitude = -1;
        //                else
        //                    dirLongitude = 1;
        //
        //
        //                //curLatitude = dirLat * ToDegree(gpsRecord.latitude);
        //                //curLongitude = dirLongitude * ToDegree(gpsRecord.longitude);
        //
        //
        //                //if ((preLatitude == 0) && (preLongtitude == 0))
        //                //{
        //
        //                //}
        //                //else
        //                //{
        //                //    //display real time GPS information here
        //                //    if ((preLatitude != 0) && (preLongtitude != 0))
        //                //    {
        //                //        kmlLineDataMakeLoad(setColor.SetBlue, 4, preLatitude, preLongtitude, curLatitude, curLongitude);
        //                //    }
        //
        //                //}
        //                //preEstLatitude = curLatitude;
        //                //preEstLongitude = curLongitude;
        //
        //                //preLatitude = curLatitude;
        //                //preLongtitude = curLongitude;
        //            }
        //        }

        #endregion

        #endregion
    }
}