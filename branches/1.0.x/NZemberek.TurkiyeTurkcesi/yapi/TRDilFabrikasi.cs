﻿using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using net.zemberek.yapi;
using net.zemberek.yapi.ek;
using net.zemberek.bilgi.kokler;
using net.zemberek.yapi.kok;
using net.zemberek.tr.yapi;
using net.zemberek.tr.yapi.ek;
using net.zemberek.tr.yapi.kok;
using net.zemberek.islemler.cozumleme;
using net.zemberek.islemler;
using net.zemberek.tr.islemler;
using net.zemberek.bilgi.araclar;
using net.zemberek.bilgi;


namespace NZemberek.TurkiyeTurkcesi.yapi
{
    class TRDilFabrikasi : IDilFabrikasi
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private String dilAdi = "TURKIYE TURKCESI";

        private Alfabe _alfabe;
        private Sozluk sozluk;
        private DenetlemeCebi _cep;
        private CozumlemeYardimcisi yardimci;
        private EkYonetici ekYonetici;
        private KokOzelDurumBilgisi ozelDurumBilgisi;
        private HeceBulucu heceleyici;

        private String bilgiDizini;

        private String alfabeDosyaAdi;
        private String ekDosyaAdi;
        private String kokDosyaAdi;
        private String cepDosyaAdi;
        private String kokIstatistikDosyaAdi;

        private bool cepKullan = true;

        public TRDilFabrikasi()
        {
            bilgiDizini = "kaynaklar";
            alfabeDosyaAdi = dosyaAdresi("harf.txt");
            ekDosyaAdi = dosyaAdresi("ek.xml");
            kokDosyaAdi = dosyaAdresi("kokler.bin");
            cepDosyaAdi = dosyaAdresi("kelime_cebi.txt");
            kokIstatistikDosyaAdi = dosyaAdresi("kok_istatistik.bin");
        }

        private string dosyaAdresi(string dosyaAdi)
        {
            return String.Format("{0}{1}{2}", bilgiDizini, System.IO.Path.DirectorySeparatorChar, dosyaAdi);
        }

        #region DilBilgisi Members

        public bool CepKullan
        {
            set { cepKullan = value; }

        }
        public Alfabe alfabe()
        {
            if (_alfabe != null) 
            {
                return _alfabe;
            } 
            else
            {
                _alfabe = new Alfabe(alfabeDosyaAdi, "tr");
                return _alfabe;
            }
        }

        public EkYonetici ekler()
        {
            if (ekYonetici != null) 
            {
                return ekYonetici;
            } 
            else
            {
                ekYonetici = new TemelEkYonetici(alfabe(), ekDosyaAdi, new EkUreticiTr(alfabe()), new TurkceEkOzelDurumUretici(alfabe()), baslangiEkAdlari());
                return ekYonetici;
            } 
        }

        public Sozluk kokler()
        {
            if (sozluk != null)
            {
                return sozluk;
            }

            if (!KaynakYukleyici.kaynakMevcutmu(kokDosyaAdi))
            {
                logger.Error("Kök dosyası bulunamadı, sozluk uretilemiyor.");
                throw new ApplicationException("Kök dosyası bulunamadı.");
            }
            kokOzelDurumlari();
            logger.Info("Ikili okuyucu uretiliyor:");
            try
            {
                KokOkuyucu okuyucu = new IkiliKokOkuyucu(kokDosyaAdi, ozelDurumBilgisi);
                logger.Info("Sozluk ve agac uretiliyor:" + dilAdi);
                okuyucu.Ac();
                sozluk = new AgacSozluk(okuyucu, alfabe(), ozelDurumBilgisi);
            }
            catch (Exception e)
            {
                logger.Error("sozluk uretilemiyor. Hata : " + e.Message);
                throw new ApplicationException("sozluk uretilemiyor. Hata : " + e.Message);
            }
            return sozluk;
        }

        public KokOzelDurumBilgisi kokOzelDurumlari()
        {
            if (ozelDurumBilgisi != null)
            {
                return ozelDurumBilgisi;
            }
            else
            {
                ozelDurumBilgisi = new TurkceKokOzelDurumBilgisi(ekler(),alfabe());
                return ozelDurumBilgisi;
            }
        }

        public HeceBulucu heceBulucu()
        {
            if (heceleyici != null)
            {
                return heceleyici;
            }
            else
            {
                heceleyici = new TurkceHeceBulucu();
                return heceleyici;
            }
        }

        public CozumlemeYardimcisi cozumlemeYardimcisi()
        {
            if (yardimci != null)
            {
                return yardimci;
            }
            else
            {
                yardimci = new TurkceCozumlemeYardimcisi(alfabe(), _cep);
                return yardimci;
            }
        }

        #endregion

        public DenetlemeCebi cep()
        {
            if (!cepKullan)
            {
                logger.Info("cep kullanilmayacak.");
                return null;
            }

            if (_cep != null)
            {
                return _cep;
            }
            else
            {
                try
                {
                    _cep = new BasitDenetlemeCebi(cepDosyaAdi);
                }
                catch (System.IO.IOException e)
                {
                    logger.Warn("cep dosyasina (" + cepDosyaAdi + ") erisilemiyor. sistem cep kullanmayacak. Hata : " + e.Message);
                    _cep = null;
                }
            }
            return _cep;
        }

        private IDictionary<KelimeTipi, String> baslangiEkAdlari()
        {
            IDictionary<KelimeTipi, String> baslangicEkAdlari = new Dictionary<KelimeTipi, String>();
            baslangicEkAdlari.Add(KelimeTipi.ISIM, TurkceEkAdlari.ISIM_KOK);
            baslangicEkAdlari.Add(KelimeTipi.SIFAT, TurkceEkAdlari.ISIM_KOK);
            baslangicEkAdlari.Add(KelimeTipi.FIIL, TurkceEkAdlari.FIIL_KOK);
            baslangicEkAdlari.Add(KelimeTipi.ZAMAN, TurkceEkAdlari.ZAMAN_KOK);
            baslangicEkAdlari.Add(KelimeTipi.ZAMIR, TurkceEkAdlari.ZAMIR_KOK);
            baslangicEkAdlari.Add(KelimeTipi.SAYI, TurkceEkAdlari.SAYI_KOK);
            baslangicEkAdlari.Add(KelimeTipi.SORU, TurkceEkAdlari.SORU_KOK);
            baslangicEkAdlari.Add(KelimeTipi.UNLEM, TurkceEkAdlari.UNLEM_KOK);
            baslangicEkAdlari.Add(KelimeTipi.EDAT, TurkceEkAdlari.EDAT_KOK);
            baslangicEkAdlari.Add(KelimeTipi.BAGLAC, TurkceEkAdlari.BAGLAC_KOK);
            baslangicEkAdlari.Add(KelimeTipi.OZEL, TurkceEkAdlari.OZEL_KOK);
            baslangicEkAdlari.Add(KelimeTipi.IMEK, TurkceEkAdlari.IMEK_KOK);
            baslangicEkAdlari.Add(KelimeTipi.YANKI, TurkceEkAdlari.YANKI_KOK);
            baslangicEkAdlari.Add(KelimeTipi.KISALTMA, TurkceEkAdlari.ISIM_KOK);
            return baslangicEkAdlari;
        }
    }
}