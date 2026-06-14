using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace aiterate.energy.web.Models
{
    public class ExternalItem
    {
        [JsonPropertyName("unique_id")]
        [Display(Name = "ID")]
        public string UniqueId { get; set; }

        [JsonPropertyName("type")]
        [Display(Name = "Type")]
        public string Type { get; set; }

        [JsonPropertyName("timestamp")]
        [Display(Name = "Moment")]
        public string Timestamp { get; set; }

        [JsonPropertyName("value")]
        [Display(Name = "Waarde")]
        public double Value { get; set; }

        [JsonPropertyName("unit")]
        [Display(Name = "Eenheid")]
        public string Unit { get; set; }
    }

    /// <summary>
    /// Meting van energie, vermogen, spanning en stroom van een HomeWizard P1 meter.
    /// 
    /// TARIEFSYSTEEM (T1/T2):
    /// Nederlandse energiemeters werken met een tweetal energietarieven:
    /// - T1 (Normaal): Standaard dagtarief, meestal 07:00-23:00 uur
    /// - T2 (Dal): Laag nachtarief, meestal 23:00-07:00 uur en weekends
    /// 
    /// Het systeem kunnen tot 3 tarieven hebben (T3 is zelden; wordt niet gebruikt voor elektriciteit,
    /// maar soms voor gas of speciale doeleinden). Deze DTO bevat alleen T1 en T2.
    /// 
    /// Het veld "Huidigtarief" geeft aan welk tarief momenteel actief is (1=T1, 2=T2).
    /// 
    /// FASE-IDENTIFICATIE (3-fasen meterkasten):
    /// Deze meter leest alle drie fasen uit. Elke fase wordt aangeduid met L1, L2, L3.
    /// 
    /// Hoe je fasen kunt identificeren:
    /// 1. In je meterkast staan L1, L2, L3 meestal al gelabeld op de spanningsrails
    /// 2. Spanning controleren: VoltageL1V/L2V/L3V moeten allen ~230V zijn
    /// 3. Belastingverdeling: PowerL1W, PowerL2W, PowerL3W moeten min of meer gelijk zijn
    ///    - Grote verschillen wijzen op ongunstige verdeling (bijv. alles op L1)
    /// 4. Test met apparaat: Zet een sterke verbruiker aan en zie welke L-waarde stijgt
    /// 5. HomeWizard app: Verify fase-toewijzing in de mobiele app
    /// 
    /// Per fase heb je: Spanning (V), Stroom (A), Vermogen (W), en kwaliteitsindicators.
    /// </summary>
    public class HomeWizardMeasurement
    {
        [JsonPropertyName("unique_id")]
        [Display(Name = "Meter-ID")]
        public string UniqueId { get; set; }

        [JsonPropertyName("protocol_version")]
        [Display(Name = "Protocol")]
        public int ProtocolVersion { get; set; }

        [JsonPropertyName("meter_model")]
        [Display(Name = "Metermodel")]
        public string MeterModel { get; set; }

        [JsonPropertyName("timestamp")]
        [Display(Name = "Moment")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Huidigtarief: Welk tarief momenteel actief is (1 = T1/Normaal, 2 = T2/Dal).
        /// Dit bepaalt welke teller wordt verhoogd bij de huidige meting.
        /// </summary>
        [JsonPropertyName("tariff")]
        [Display(Name = "Huidigtarief")]
        public int Tariff { get; set; }

        [JsonPropertyName("energy_import_kwh")]
        [Display(Name = "Totaal verbruik (kWh)")]
        public double EnergyImportKwh { get; set; }

        /// <summary>
        /// T1 (Normaal): Standaard dagtarief, meestal van 07:00 tot 23:00 uur.
        /// Dit is het reguliere verbruikstarief voor normale bedrijfsuren.
        /// </summary>
        [JsonPropertyName("energy_import_t1_kwh")]
        [Display(Name = "Verbruik normaal (T1) (kWh)")]
        public double EnergyImportT1Kwh { get; set; }

        /// <summary>
        /// T2 (Dal): Laag/dal tarief, meestal van 23:00 tot 07:00 uur en weekends.
        /// Dit is het gunstigere tarief voor nachtverbruik en weekendgebruik.
        /// </summary>
        [JsonPropertyName("energy_import_t2_kwh")]
        [Display(Name = "Verbruik dal (T2) (kWh)")]
        public double EnergyImportT2Kwh { get; set; }

        [JsonPropertyName("energy_export_kwh")]
        [Display(Name = "Totaal teruglevering (kWh)")]
        public double EnergyExportKwh { get; set; }

        /// <summary>
        /// T1 (Normaal): Standaard dagtarief voor teruglevering (als je zonnepanelen hebt).
        /// </summary>
        [JsonPropertyName("energy_export_t1_kwh")]
        [Display(Name = "Teruglevering normaal (T1) (kWh)")]
        public double EnergyExportT1Kwh { get; set; }

        /// <summary>
        /// T2 (Dal): Dal-tarief voor teruglevering, meestal nachturen en weekends.
        /// </summary>
        [JsonPropertyName("energy_export_t2_kwh")]
        [Display(Name = "Teruglevering dal (T2) (kWh)")]
        public double EnergyExportT2Kwh { get; set; }

        /// <summary>
        /// Actueel vermogen (totaal over alle drie fasen in Watt).
        /// Dit is de som van PowerL1W + PowerL2W + PowerL3W.
        /// </summary>
        [JsonPropertyName("power_w")]
        [Display(Name = "Actueel vermogen (W)")]
        public int PowerW { get; set; }

        /// <summary>
        /// Vermogen Fase 1 (in Watt).
        /// L1, L2, L3 corresponderen met de drie fasen in je meterkast.
        /// Vergelijk met PowerL2W en PowerL3W om belastingverdeling te controleren.
        /// </summary>
        [JsonPropertyName("power_l1_w")]
        [Display(Name = "Vermogen L1 (W)")]
        public int PowerL1W { get; set; }

        /// <summary>
        /// Vermogen Fase 2 (in Watt).
        /// Ideaal: ongeveer gelijk aan PowerL1W en PowerL3W voor gebalanceerde belasting.
        /// </summary>
        [JsonPropertyName("power_l2_w")]
        [Display(Name = "Vermogen L2 (W)")]
        public int PowerL2W { get; set; }

        /// <summary>
        /// Vermogen Fase 3 (in Watt).
        /// </summary>
        [JsonPropertyName("power_l3_w")]
        [Display(Name = "Vermogen L3 (W)")]
        public int PowerL3W { get; set; }

        /// <summary>
        /// Spanning Fase 1 (in Volt, normaal ~230V).
        /// Dit is de elektrische potentiaal tussen L1 en Nul (ground).
        /// </summary>
        [JsonPropertyName("voltage_l1_v")]
        [Display(Name = "Spanning L1 (V)")]
        public double VoltageL1V { get; set; }

        /// <summary>
        /// Spanning Fase 2 (in Volt, normaal ~230V).
        /// </summary>
        [JsonPropertyName("voltage_l2_v")]
        [Display(Name = "Spanning L2 (V)")]
        public double VoltageL2V { get; set; }

        /// <summary>
        /// Spanning Fase 3 (in Volt, normaal ~230V).
        /// Alle drie fasen moeten ongeveer gelijk zijn. 
        /// Grote verschillen wijzen op netproblemen of ongunstige belastingverdeling.
        /// </summary>
        [JsonPropertyName("voltage_l3_v")]
        [Display(Name = "Spanning L3 (V)")]
        public double VoltageL3V { get; set; }

        /// <summary>
        /// Totale stroom (som van stroom over alle fasen, in Ampère).
        /// Dit is de som van CurrentL1A + CurrentL2A + CurrentL3A.
        /// </summary>
        [JsonPropertyName("current_a")]
        [Display(Name = "Stroom (A)")]
        public double CurrentA { get; set; }

        /// <summary>
        /// Stroom Fase 1 (in Ampère).
        /// Gebruik dit samen met VoltageL1V om vermogen per fase te berekenen (P = U × I).
        /// </summary>
        [JsonPropertyName("current_l1_a")]
        [Display(Name = "Stroom L1 (A)")]
        public double CurrentL1A { get; set; }

        /// <summary>
        /// Stroom Fase 2 (in Ampère).
        /// </summary>
        [JsonPropertyName("current_l2_a")]
        [Display(Name = "Stroom L2 (A)")]
        public double CurrentL2A { get; set; }

        /// <summary>
        /// Stroom Fase 3 (in Ampère).
        /// </summary>
        [JsonPropertyName("current_l3_a")]
        [Display(Name = "Stroom L3 (A)")]
        public double CurrentL3A { get; set;}

        /// <summary>
        /// Spanningsdips Fase 1 (aantal gebeurtenissen).
        /// Een spanningsdip is een korte verlaging van de spanning beneden een drempel.
        /// Hoog aantal kan wijzen op netwerkproblemen.
        /// </summary>
        [JsonPropertyName("voltage_sag_l1_count")]
        [Display(Name = "Spanningsdips L1 (aantal)")]
        public int VoltageSagL1Count { get; set; }

        /// <summary>
        /// Spanningsdips Fase 2 (aantal gebeurtenissen).
        /// </summary>
        [JsonPropertyName("voltage_sag_l2_count")]
        [Display(Name = "Spanningsdips L2 (aantal)")]
        public int VoltageSagL2Count { get; set; }

        /// <summary>
        /// Spanningsdips Fase 3 (aantal gebeurtenissen).
        /// </summary>
        [JsonPropertyName("voltage_sag_l3_count")]
        [Display(Name = "Spanningsdips L3 (aantal)")]
        public int VoltageSagL3Count { get; set; }

        /// <summary>
        /// Spanningspieken Fase 1 (aantal gebeurtenissen).
        /// Een spanningstijging is een korte verhoging van de spanning boven een drempel.
        /// Kan gevoelige apparatuur beschadigen als frequent.
        /// </summary>
        [JsonPropertyName("voltage_swell_l1_count")]
        [Display(Name = "Spanningspieken L1 (aantal)")]
        public int VoltageSwellL1Count { get; set; }

        /// <summary>
        /// Spanningspieken Fase 2 (aantal gebeurtenissen).
        /// </summary>
        [JsonPropertyName("voltage_swell_l2_count")]
        [Display(Name = "Spanningspieken L2 (aantal)")]
        public int VoltageSwellL2Count { get; set; }

        /// <summary>
        /// Spanningspieken Fase 3 (aantal gebeurtenissen).
        /// </summary>
        [JsonPropertyName("voltage_swell_l3_count")]
        [Display(Name = "Spanningspieken L3 (aantal)")]
        public int VoltageSwellL3Count { get; set; }

        [JsonPropertyName("any_power_fail_count")]
        [Display(Name = "Stroomonderbrekingen (aantal)")]
        public int AnyPowerFailCount { get; set; }

        [JsonPropertyName("long_power_fail_count")]
        [Display(Name = "Lange stroomonderbrekingen (aantal)")]
        public int LongPowerFailCount { get; set; }

        [JsonPropertyName("external")]
        [Display(Name = "Externe meters")]
        public List<ExternalItem> External { get; set; }
    }
}