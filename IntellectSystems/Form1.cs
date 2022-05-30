using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace IntellectSystems
{
    public partial class Form1 : Form
    {
        float[][] data = new float[10][];
        float[][] diapason = new float[4][];
        float[] measures_average = new float[4];

        DataBase dataBase = new DataBase();
        public Form1()
        {
            InitializeComponent();
            createColumns();
            establishConnection();
            calculateDiapason();
        }

        private void createColumns()
        {
            dataGridView1.Columns.Add("id", "ID Электро-\nпреобразователя");
            dataGridView1.Columns.Add("measure_1", "a");
            dataGridView1.Columns.Add("measure_2", "b");
            dataGridView1.Columns.Add("measure_3", "c");
            dataGridView1.Columns.Add("measure_4", "d");
            dataGridView1.Columns.Add("points", "Баллы");
            dataGridView1.Columns.Add("malfunction", "Предполагаемая неполадка");
        }

        private void establishConnection()
        {

            dataBase.openConnection();

            SqlCommand command = new SqlCommand("SELECT * FROM measurements", dataBase.GetConnection());
            SqlDataReader reader = command.ExecuteReader();

            for (int i = 0; i < 10; i++)
            {
                reader.Read();
                data[i] = new float[5] { (float)Convert.ToDouble(reader[0]), (float)Convert.ToDouble(reader[1]), (float)Convert.ToDouble(reader[2]), (float)Convert.ToDouble(reader[3]), (float)Convert.ToDouble(reader[4]) };
                dataGridView1.Rows.Add(new object[] { data[i][0], data[i][1], data[i][2], data[i][3], data[i][4] });
            }
            
        }

        private void calculateDiapason()
        {
            float[] measures_sum = new float[4];
            float[] measures_sigma = new float[4];
            float[] sigma_coeffs = { -3, (float)-1.15, (float)-0.67, (float)-0.32, 0, (float)0.32, (float)0.67, (float)1.15, 3 };
            float sum;

            for (int i = 0; i < 4; i++)
            {
                sum = 0;
                foreach (var measure in data)
                {
                    sum += measure[i+1];
                    /*Console.WriteLine(measure[i+1]);*/
                }

                measures_average[i] = sum/10;
                /*Console.WriteLine(measures_average[i]);*/

                foreach(var measure in data)
                {
                    measures_sum[i] += (measure[i + 1] - measures_average[i]) * (measure[i + 1] - measures_average[i]);
                    /*Console.WriteLine("measure = " + measure[i+1] + " measures_average = " + measures_average[i] + " measures_sum = " + measures_sum[i]);*/
                }

                measures_sigma[i] = (float)Math.Sqrt((float)1 / (float)(data.Length - 1) * measures_sum[i]);

                diapason[i] = new float[9];
                for (int j = 0; j < 9; j++)
                {
                    diapason[i][j] = measures_sigma[i] * sigma_coeffs[j];
                }
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void calculate_points_button_Click(object sender, EventArgs e)
        {
            int points;
            int row = 0;
            int point_sample;
            float[] measures_delta = new float[10];

            foreach (var line in data)
            {
                points = 0;
                point_sample = 1000;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        if ((line[i+1] - measures_average[i]) < diapason[i][j])
                        {
                            /*Console.WriteLine("y");*/
                            if (j == 0)
                                j++;
                            points += j * point_sample;
                            break;
                        }
                    }
                    point_sample /= 10;
                }
                dataGridView1.Rows[row].Cells[5].Value = points;

                string malfunction = "";
                int points_sum = 0;
                while(points > 0)
                {
                    points_sum += (points%10);
                    points /= 10;
                }

                Console.WriteLine(points_sum);

                if (10 <= points_sum && points_sum <= 14)
                    malfunction = "Неисправность не выявлена.\nВыявленные неточности в показаниях датчиков -\nнезначительная погрешность.";
                else if (15 <= points_sum && points_sum <= 19)
                    malfunction = "Двигатель преобразователя не вращается с нужной скоростью.\nПричиной может быть неверное задание частоты,\nлибо слишком большая нагрузка на двигатель.";
                else if (20 <= points_sum && points_sum <= 24)
                    malfunction = "Нестабильность напряжения в питающей сети.\nВозможна ошибки монтажа и коммутации преобразователя частоты и электромотора";
                else if (25 <= points_sum && points_sum <= 29)
                    malfunction = "Несоответствие нагрузочного цикла.\nВероятен пробой конденсатора или транзисторного блока.";
                else if (points_sum < 30)
                    malfunction = "Преобразователь не функционирует должным образом.\nВероятен выход из строя любого из компонентов.";

                dataGridView1.Rows[row].Cells[6].Value = malfunction;
                row++;
            }
        }
    }
}
