﻿using AnkaKafe.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnkaKafe.UI
{
    public partial class SiparisForm : Form
    {
        // EVENT OLUŞTURMADA 2. ADIM
        // EventHandler delegesi ile event oluşturulur
        // Eğer event ile ilgili argümanlar varsa generic olan kullanılır
        public event EventHandler<MasaTasindiEventArgs> MasaTasindi;

        private readonly KafeVeri _db;
        private readonly Siparis _siparis;
        private readonly BindingList<SiparisDetay> _blSiparisDetaylar;

        // yeni bir sipariş formu oluştururken bu parametreler zorunlu
        public SiparisForm(KafeVeri kafeVeri, Siparis siparis)
        {
            _db = kafeVeri;
            _siparis = siparis;
            _blSiparisDetaylar = new BindingList<SiparisDetay>(siparis.SiparisDetaylar);
            InitializeComponent();
            dgvSiparisDetaylar.AutoGenerateColumns = false; // otomatik sü
            UrunleriGoster();
            EkleFormSifirla();
            MasaNoGuncelle();
            FiyatGuncelle();
            DetaylariListele();
            MasaNolariDoldur();
            _blSiparisDetaylar.ListChanged += _blSiparisDetaylar_ListChanged;
        }

        private void MasaNolariDoldur()
        {
            //List<int> bosMasaNolar = new List<int>();

            //for (int i = 1; i <= _db.MasaAdet; i++)
            //{
            //    // aktif siparişlerde i masa nosuna sahip sipariş var DEĞİLSE / yoksa
            //    if(!_db.AktifSiparisler.Any(x => x.MasaNo == i))
            //    {
            //        bosMasaNolar.Add(i);
            //    }
            //}

            //cboMasaNo.DataSource = bosMasaNolar;

            cboMasaNo.DataSource = Enumerable
                .Range(1, 20)
                .Where(i => !_db.AktifSiparisler.Any(s => s.MasaNo == i))
                .ToList();
        }

        private void _blSiparisDetaylar_ListChanged(object sender, ListChangedEventArgs e)
        {
            FiyatGuncelle();
        }

        private void UrunleriGoster()
        {
            cboUrun.DataSource = _db.Urunler;
        }

        private void FiyatGuncelle()
        {
            lblOdemeTutar.Text = _siparis.ToplamTutarTL;
        }

        private void MasaNoGuncelle()
        {
            Text = $"Masa {_siparis.MasaNo} Sipariş Bilgileri";
            lblMasaNo.Text = _siparis.MasaNo.ToString("");
        }

        private void btnEkle_Click(object sender, EventArgs e)
        {
            if (cboUrun.SelectedIndex == -1 || nudAdet.Value < 1) 
                return; // seçili ürün yok, metottan çık

            Urun urun = (Urun)cboUrun.SelectedItem;

            SiparisDetay siparisDetay = new SiparisDetay()
            {
                UrunAd = urun.UrunAd,
                BirimFiyat = urun.BirimFiyat,
                Adet = (int)nudAdet.Value
            };

            // _blSiparisDetaylar içinde _siparis.SiparisDetaylar'ı da içerdiği için
            // aynı zamanda Form'dan gelen _siparis nesnesinin detaylarına da bu detayı ekleyecektir
            // ve datagridview'ı kendindeki verilerin değiştiği konusunda bilgilendirecektir.
            _blSiparisDetaylar.Add(siparisDetay);
            EkleFormSifirla();
        }

        private void EkleFormSifirla()
        {
            cboUrun.SelectedIndex = -1;
            nudAdet.Value = 1;
        }

        private void DetaylariListele()
        {
            dgvSiparisDetaylar.DataSource = _blSiparisDetaylar;
        }

        private void dgvSiparisDetaylar_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                text : "Seçili sipariş detayları silinecektir. Onaylıyor musun?",
                caption : "Silme Olayı",
                buttons : MessageBoxButtons.YesNo,
                icon : MessageBoxIcon.Exclamation,
                defaultButton :MessageBoxDefaultButton.Button2
            );

            // true atamanız sonucunda satır silme işleminin önüne geçmiş olursunuz
            e.Cancel = dr == DialogResult.No;
        }

        private void btnAnasayfa_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnIptal_Click(object sender, EventArgs e)
        {
            SiparisKapat(SiparisDurum.Iptal, 0);
        }

        private void btnOde_Click(object sender, EventArgs e)
        {
            SiparisKapat(SiparisDurum.Odendi, _siparis.ToplamTutar());
        }

        private void SiparisKapat(SiparisDurum siparisDurum, decimal odenenTutar)
        {
            _siparis.OdenenTutar = odenenTutar;
            _siparis.Durum = siparisDurum;
            _siparis.KapanisZamani = DateTime.Now;
            _db.AktifSiparisler.Remove(_siparis);
            _db.GecmisSiparisler.Add(_siparis);
            Close();
        }

        private void btnTasi_Click(object sender, EventArgs e)
        {
            if (cboMasaNo.SelectedIndex == -1) return;

            int eskiMasaNo = _siparis.MasaNo;
            int yeniMasaNo = (int)cboMasaNo.SelectedItem;
            _siparis.MasaNo = yeniMasaNo;
            MasaNolariDoldur(); // dolu masalar değişti

            // EVENT OLUŞTURMADA 3. ADIM
            // event'e atanmış bir metot var ise uygun noktada uygun argümanlarla o metot çağrılır
            if (MasaTasindi != null)
            {
                MasaTasindi(this, new MasaTasindiEventArgs(eskiMasaNo, yeniMasaNo));
            }
            MasaNoGuncelle();
        }
    }
}
