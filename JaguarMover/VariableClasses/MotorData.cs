namespace JaguarMover
{
    public class MotorData
    {
        public double motAmp1;
        public double motAmp2;
        public int motPower1;
        public int motPower2;
        public int motEncP1;
        public int motEncP2;
        public int motEncS1;
        public int motEncS2;
        public double motTemp1;
        public double motTemp2;
        public double drvVoltage;
        public double batVoltage;
        public double reg5VVoltage;
        public double ai3;
        public double ai4;
        public int di3;
        public int di4;
        public int do1;
        public int do2;
        public double intTemp;
        public double ch1Temp;
        public double ch2Temp;
        public int statusFlag;
        public int mode1;
        public int mode2;
        public int motEncCR1;
        public int motEncCR2;
        public string erorMsg;

        public MotorData()
        {
            motAmp1 = 0;
            motAmp2 = 0;
            motPower1 = 0;
            motPower2 = 0;
            motEncP1 = 0;
            motEncP2 = 0;
            motEncS1 = 0;
            motEncS2 = 0;
            motTemp1 = 0;
            motTemp2 = 0;
            drvVoltage = 0;
            batVoltage = 0;
            reg5VVoltage = 0;
            ai3 = 0;
            ai4 = 0;
            di3 = 0;
            di4 = 0;
            do1 = 0;
            do2 = 0;
            intTemp = 0;
            ch1Temp = 0;
            ch2Temp = 0;
            statusFlag = 0;
            mode1 = 0;
            mode2 = 0;
            motEncCR1 = 0;
            motEncCR2 = 0;
            erorMsg = "";
        }
    }
}