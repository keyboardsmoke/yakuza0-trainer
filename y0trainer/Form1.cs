using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace y0trainer
{
    public partial class Form1 : Form
    {
        y0trainer.Memory mem = new y0trainer.Memory("Yakuza0");

        // Settings
        bool kiryu_money = false;
        bool majima_money = false;

        bool kiryu_cp = false;
        bool majima_cp = false;

        bool infinite_hostess_special = false;

        // State
        bool process_open = false;

        public Form1()
        {
            InitializeComponent();
        }

        private bool giveItem(UInt16 itemId)
        {
            // max items
            if (itemId > 963)
            {
                return false;
            }

            return true;
        }

        private bool checkProcessState()
        {
            bool initial_process_state = process_open;

            if (!mem.Valid())
            {
                if (!mem.Open())
                {
                    process_open = false;
                }
                else
                {
                    process_open = true;
                }
            }
            else
            {
                process_open = true;
            }

            if (process_open != initial_process_state)
            {
                if (!process_open)
                {
                    this.processStateLabel.Text = "Yakuza 0 is not open...";
                    this.kiryu_money_btn.Enabled = false;
                    this.kiryu_cp_btn.Enabled = false;
                    this.majima_money_btn.Enabled = false;
                    this.majima_cp_btn.Enabled = false;
                    this.give_item_btn.Enabled = false;
                    this.comboBox1.Enabled = false;
                    this.infinite_hostess_special_btn.Enabled = false;
                    this.give_all_materials_btn.Enabled = false;
                    this.give_pocket_racer_items_btn.Enabled = false;
                }
                else
                {
                    this.processStateLabel.Text = "Yakuza 0 is open.";
                    this.kiryu_money_btn.Enabled = true;
                    this.kiryu_cp_btn.Enabled = true;
                    this.majima_money_btn.Enabled = true;
                    this.majima_cp_btn.Enabled = true;
                    this.give_item_btn.Enabled = true;
                    this.comboBox1.Enabled = true;
                    this.infinite_hostess_special_btn.Enabled = true;
                    this.give_all_materials_btn.Enabled = true;
                    this.give_pocket_racer_items_btn.Enabled = true;
                }
            }

            return process_open;
        }

        public void handleStatistics()
        {
            IntPtr MainAddress = new IntPtr(0x1413159C8);
            //
            //IntPtr MainPointer = mem.Ptr(MainAddress);
            IntPtr GamePointer = mem.Ptr(MainAddress);
            IntPtr StatsPointer = IntPtr.Add(GamePointer, 0xF420);
            IntPtr KiryuMoney = IntPtr.Add(StatsPointer, 0x0008);
            IntPtr MajimaMoney = IntPtr.Add(StatsPointer, 0x0010);

            IntPtr KiryuCP = IntPtr.Add(StatsPointer, 0x0128);
            IntPtr MajimaCP = IntPtr.Add(StatsPointer, 0x0130);

            if (kiryu_money)
                mem.U64(KiryuMoney, 9999999999);

            if (majima_money)
                mem.U64(MajimaMoney, 9999999999);

            if (kiryu_cp)
                mem.U32(KiryuCP, 100);

            if (majima_cp)
                mem.U32(MajimaCP, 100);
        }

        // 14131E210 = hostess base or something
        // +180
        // +44 = money earned?
        // +48 = special charge
        private void handleHostessClub()
        {
            if (!infinite_hostess_special)
                return;

            IntPtr HostessBase = new IntPtr(0x14131E210);
            IntPtr HostessPointer = mem.Ptr(HostessBase);
            if (HostessPointer == IntPtr.Zero)
                return;

            IntPtr ClubPointer = mem.Ptr(IntPtr.Add(HostessPointer, 0x180));
            if (ClubPointer == IntPtr.Zero)
                return;

            IntPtr ClubSpecialChargePointer = IntPtr.Add(ClubPointer, 0x48);

            mem.Float(ClubSpecialChargePointer, 100.0f);
        }

        private void trainerTimer_Tick(object sender, EventArgs e)
        {
            if (!checkProcessState())
                return;

            // We are safe to edit memory here, since we would have returned otherwise.
            try
            {
                handleStatistics();
                handleHostessClub();
            }
            catch (Exception)
            {
                // We have to check process state here, in case the exception was generated
                // by the process closing
                if (!checkProcessState())
                    return;
            }
        }

        private void kiryu_money_btn_Click(object sender, EventArgs e)
        {
            kiryu_money = !kiryu_money;
            kiryu_money_btn.Text = (kiryu_money) ? "Infinite Money On" : "Infinite Money Off";
        }

        private void kiryu_cp_btn_Click(object sender, EventArgs e)
        {
            kiryu_cp = !kiryu_cp;
            kiryu_cp_btn.Text = (kiryu_cp) ? "Infinite CP On" : "Infinite CP Off";
        }

        private void majima_money_btn_Click(object sender, EventArgs e)
        {
            majima_money = !majima_money;
            majima_money_btn.Text = (majima_money) ? "Infinite Money On" : "Infinite Money Off";
        }

        private void majima_cp_btn_Click(object sender, EventArgs e)
        {
            majima_cp = !majima_cp;
            majima_cp_btn.Text = (majima_cp) ? "Infinite CP On" : "Infinite CP Off";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            trainerTimer.Enabled = true;

            comboBox1.DataSource = Enum.GetValues(typeof(y0trainer.ItemID));
        }

        private void processStateLabel_Click(object sender, EventArgs e)
        {

        }

        private void giveItem(uint itemId)
        {
            try
            {
                IntPtr itemIdArgument = new IntPtr((uint)itemId);
                if (!mem.CallAddress(0x1403E5BE0, itemIdArgument))
                {
                    System.Windows.Forms.MessageBox.Show("Failed to give item.");
                }
            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.ToString());
            }
        }

        private void give_item_btn_Click(object sender, EventArgs e)
        {
            if (!checkProcessState())
                return;

            y0trainer.ItemID itemId;
            Enum.TryParse<y0trainer.ItemID>(comboBox1.SelectedValue.ToString(), out itemId);

            try
            {
                giveItem((uint)itemId);
            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.ToString());
            }
        }

        private void give_pocket_racer_items_btn_Click(object sender, EventArgs e)
        {
            uint start = (uint)ItemID.pokecir_sticker_a;
            uint end = (uint)ItemID.pokecir_suspension_03;

            try
            {
                for (uint i = start; i < end + 1; i++)
                {
                    giveItem(i);
                }
            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.ToString());
            }
        }

        private void give_all_materials_btn_Click(object sender, EventArgs e)
        {
            uint start = (uint)ItemID.material001;
            uint end = (uint)ItemID.material118;

            try
            {
                for (uint i = start; i < end + 1; i++)
                {
                    giveItem(i);
                }
            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.ToString());
            }
        }

        private void infinite_hostess_special_btn_Click(object sender, EventArgs e)
        {
            infinite_hostess_special = !infinite_hostess_special;
            infinite_hostess_special_btn.Text = (infinite_hostess_special) ? "Infinite Hostess Special On" : "Infinite Hostess Special Off";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IntPtr result = y0trainer.Pattern.Find(mem,
                "\x48\x81\xC1\x00\x01\x00\x00\xE8\x00\x00\x00\x00\x48\x8B\x05", 
                "xxxxxxxx????xxx");

            if (result != IntPtr.Zero)
            {
                result = IntPtr.Add(result, 0xF);
                UInt32 offset = mem.U32(result);
                result = IntPtr.Add(result, (int)(offset + 4));

                System.Windows.Forms.MessageBox.Show("Address: " + result.ToInt64().ToString("X"));
            }
        }
    }
}
