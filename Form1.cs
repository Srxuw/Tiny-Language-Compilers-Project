using System;
using System.Windows.Forms;

namespace JASON_Compiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            textBox2.Clear();

            Scanner scanner = new Scanner();
            scanner.StartScanning(textBox1.Text);

            foreach (var token in JASON_Compiler.TokenStream)
            {
                dataGridView1.Rows.Add(token.lex, token.token_type);
            }

            // Append errors instead of replacing text
            textBox2.AppendText(Errors.GetAllErrors());
        }


        void PrintTokens()
        {
            foreach (var token in JASON_Compiler.TokenStream)
            {
                dataGridView1.Rows.Add(token.lex, token.token_type.ToString());
            }
        }

        void PrintErrors()
        {
            foreach (var token in JASON_Compiler.TokenStream)
            {
                if (token.token_type == Token_Class.Undefined)
                {
                    textBox2.AppendText($"Unrecognized token: {token.lex}\r\n");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            textBox1.Clear();
            textBox2.Clear();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            // Optional
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Optional
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // Optional
        }
    }
}
