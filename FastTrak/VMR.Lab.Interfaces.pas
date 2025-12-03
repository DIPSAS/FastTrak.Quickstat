unit VMR.Lab.Interfaces;

interface

uses
  VMR.Common.Interfaces;

type
  { TDevIndicator matches OID 8244 from KITH, and is used to specify abnormal lab results. }

  TDevIndicator = ( diNone, diHigh, diLow, diGeneral );

  { Arithmetic comparator matches OID 8239 from KITH, used to specify lab results without exact values }

  TArithmeticComp = ( acNone, acEqual, acGreaterOrEqual, acGreater, acLessOrEqual, acLess, acMuchGreater, acMuchLess, acNotEqual, acApproximate );

  { Groups of lab tests, Emetra AS proprietray codes }
  TLabGroup = ( lgNone, lgUrine, lgBP, lgVitals, lgHematology, lgLipids, lgSpiro, lgSmoking, lgGlucose, lgInterviews );

  { Enumeration of Lab tests.  Only Add at the end.  See http://EmetraLab.azurewebsites.net/LabClass.asp }

  TLabTest = ( ltUnclassified, ltNotLab, ltFaecal,
    { Urine tests }
    ltDUProtein, ltUAlbumin, ltUMicroAlbumin, ltACRatio, ltDUAlbumin,
    { Vitals }
    ltHeight, ltWeight, ltBMI, ltWaist, ltHip, ltBP, ltSBP, ltDBP, ltHeartRate,
    { Coagulation }
    ltaPTT, ltTT, ltNT, ltINR, lFibrinogen,
    { Blood cells }
    ltHGB, ltWBC, ltRBC, ltESR, ltCRP, ltEVF, ltTPC, ltMCV, ltMCHC, ltMCH, ltCELLCOUNT, ltBLOODSMEAR,
    { Lipids }
    ltCholesterol, ltLdlCholesterol, ltTriglycerides, ltHdlCholesterol, ltCholRatio, ltLipoSmallA, ltApoLipoProtein,
    { Glucose }
    ltInsuline, ltFastGlucose, ltPlasmaGlucose, ltBloodGlucose, ltHBA1C, ltGlucose30min, ltGlucose90min, ltCPeptide,
    { Kidney }
    ltCreatinine, ltEstGFR, ltCockcroftGault, ltMDRD, ltUrate, ltUrea,
    { Proteins }
    ltAlbumine, ltElectrophoresis, ltTotalProt, ltGlucose0min, ltGlucose60min, ltGlucose120min, ltVitaminK,
    { Anemia }
    ltB12, ltPFolate, ltEryFolate, ltHomocysteine, ltMethylmalonate,
    { Hormones }
    ltProlactine, ltProgesterone, ltEstradiol, ltTestosteron, ltFSH, ltLH, ltSHBG, ltHCGUrine, ltCalcitonine, ltHCGSerum,
    { Iron metabolism }
    ltTIBC, ltFerritine, ltIron, ltTransferrine, ltIronBind, ltIronSaturation,
    { Thyroid function }
    ltTSH, ltFT4, ltT4, ltFT3, ltT3, ltTPOAS, ltTRAS,
    { Electrolytes }
    ltNatrium, ltKalium, ltCalcium, ltChloride, ltMagnesium, ltPhosphate, ltLithium,
    { Various infectious agents }
    ltCryoglobulin, ltHepatitis, ltHIV, ltChlamydiaPn, ltVaricella, ltInfluenza, ltCytomegalovirus, ltEBV, ltLUES, ltRUBELLA, ltYersinia, ltBorrelia,
    ltBACTERIA, ltVIRUS,
    { Allergies }
    ltAllergyFoodpanel, ltAllergyPets, ltANTIPOLLEN, ltLACTOSETEST,
    { Rheumatology }
    ltRA, ltANA,
    { Unspecific immunology }
    ltIGG, ltIGA, ltIGF1, ltIGM, ltAB0, ltTopiramate,
    { Liver Enzymes etc. }
    ltAlcalicPhosphatase, ltALAT, ltASAT, ltCK, ltCKMB, ltGammaGT, ltBilirubineTotal, ltLD,
    { Other enzymes }
    ltAmylase, ltPancreasAmylase, ltAcidicPhosphatase,
    { Spirometry }
    ltFEV, ltFEV1, ltFEV1PERC, ltFVC, ltPEF,
    { Heart failure }
    ltLeftVentricularEjectionFraction, ltProBNP_pMol,
    { Smoking }
    ltGramTobacco, ltSMKCLAS, ltSMOKER, ltQuitMotivation, ltSmartDiet, ltMADRS, ltICPC, ltICD10,
    { Drinking }
    ltEthanol, ltCDT,
    { months }
    ltHour, ltMonth, ltSeason,
    { Drugs }
    ltDrugs, ltBPDrug, ltAntibiotics, ltValproate,
    { Opioids etc }
    ltAnalgesics, ltNarcotics,
    { Family history }
    ltFAMCHD,
    { Tests }
    ltPSA,
    { Gang-i-GEN custom variables }
    ltWALKDIST, ltATC_B01AC, ltATC_C07, ltATC_C09, ltATC_C10A, ltABILEFT, ltABIRIGHT, ltFOOTPULSELEFT, ltFOOTPULSERIGHT,
    { More drugs }
    ltDigitoxin, ltDDimer, ltTroponinT, ltTroponinI, ltLast );

  TLabSet = set of TLabTest;

  { Predefined sets of labdata }
const
  LABSET_UNSPEC = [ltUnclassified, ltNotLab, ltDrugs, ltCELLCOUNT, ltElectrophoresis, ltHepatitis, ltVIRUS, ltAllergyFoodpanel, ltANTIPOLLEN, ltApoLipoProtein,
    ltLast, ltFaecal];

  LABSET_BP            = [ltSBP, ltDBP, ltBP];
  LABSET_LIPIDS        = [ltCholesterol .. ltApoLipoProtein];
  LABSET_DIGITALIS     = [ltKalium, ltALAT, ltDigitoxin, ltProBNP_pMol];
  LABSET_URINE         = [ltDUAlbumin, ltUAlbumin, ltUMicroAlbumin, ltACRatio, ltDUProtein];
  LABSET_GLUCOSE       = [ltInsuline .. ltCPeptide, ltGlucose0min, ltGlucose30min, ltGlucose60min, ltGlucose90min, ltGlucose120min];
  LABSET_HEMATOLOGY    = [ltHGB .. ltBLOODSMEAR];
  LABSET_DIABETES      = LABSET_GLUCOSE + [ltUrate, ltEstGFR, ltCreatinine] + [ltDUAlbumin, ltUAlbumin, ltUMicroAlbumin, ltACRatio];
  LABSET_INR           = [ltTT, ltINR];
  LABSET_SMOKING       = [ltGramTobacco, ltSMOKER, ltSMKCLAS, ltQuitMotivation];
  LABSET_VITALS        = [ltHeight .. ltHeartRate];
  LABSET_SPIROMETRY    = [ltFEV .. ltPEF];
  LABSET_KIDNEY        = [ltUrate, ltUrea, ltEstGFR, ltCreatinine, ltNatrium, ltKalium] + LABSET_URINE;
  LABSET_LIVER         = [ltAlcalicPhosphatase .. ltLD];
  LABSET_THYROID       = [ltTSH .. ltTRAS];
  LABSET_HOSPITAL      = [ltLeftVentricularEjectionFraction];
  LABSET_DX            = [ltICPC, ltICD10];
  LABSET_GANGIGEN      = [ltWALKDIST .. ltFOOTPULSERIGHT];
  LABSET_ALL           = [ltFaecal .. ltFOOTPULSERIGHT];
  LABSET_YNU           = [ltFOOTPULSELEFT, ltFOOTPULSERIGHT, ltSMOKER];
  LABSET_TESTS         = [ltSmartDiet, ltMADRS];
  LABSET_GFR           = [ltEstGFR, ltCockcroftGault, ltMDRD];
  LABSET_NDV_LIPIDS    = [ltCholesterol, ltLdlCholesterol, ltHdlCholesterol];
  LABSET_NDV_KIDNEY    = [ltUMicroAlbumin, ltACRatio, ltCreatinine, ltEstGFR];
  LABSET_HEART_FAILURE = [ltACRatio, ltHGB, ltCreatinine, ltUrate, ltNatrium] + LABSET_DIGITALIS + LABSET_GFR;

const
  ltFirst                                                 = ltUnclassified;
  LAB_GROUP_NAMES: array [TLabGroup] of string            = ( '*', 'URINE', 'BP', 'VITAL', 'HEMAT', 'LIPIDS', 'SPIRO', 'SMOKING', 'GLUCOSE', 'INTERVIEW' );
  ARITHMETIC_COMP_STR: array [TArithmeticComp] of string  = ( '', '=', '>=', '>', '<=', '<', '>>', '<<', '<>', '~' );
  ARITHMETIC_COMP_HTML: array [TArithmeticComp] of string = ( '', '=', '&gt;=', '&gt;', '&lt;=', '&lt;', '&gt;&gt;', '&lt;&lt;', '&lt;&gt;', 'ca' );
  DEV_IND_STR: array [TDevIndicator] of char              = ( ' ', 'H', 'L', '*' );

type
  ILabClass = interface
    ['{EA607DBA-1865-4621-841D-D7800355C55F}']
    { Accessors }
    function Get_ClassifyWithUnit: boolean;
    function Get_FriendlyName: string;
    function Get_FurstId: integer;
    function Get_IsGroup: boolean;
    function Get_LabTest: TLabTest;
    function Get_LabClassId: integer;
    function Get_LoincCode: string;
    function Get_NLK: string;
    function Get_TrustLevel: integer;
    function Get_UnitStr: string;
    function Get_VarName: string;
    { Other members }
    function IsValidResult( const AValue: double ): boolean;
    { Properties }
    property ClassifyWithUnit: boolean read Get_ClassifyWithUnit;
    property FriendlyName: string read Get_FriendlyName;
    property FurstId: integer read Get_FurstId;
    property IsGroup: boolean read Get_IsGroup;
    property LoincCode: string read Get_LoincCode;
    property LabClassId: integer read Get_LabClassId;
    property LabTest: TLabTest read Get_LabTest;
    property NLK: string read Get_NLK;
    property UnitStr: string read Get_UnitStr;
    property TrustLevel: integer read Get_TrustLevel;
    property VarName: string read Get_VarName;
  end;

  ILabEntry = interface( IVmrFragment )
    ['{B507B70E-2CB5-4A3B-BA0A-302D754E1EE6}']
    function Get_ArithmeticComp: TArithmeticComp;
    function Get_Caption: string;
    function Get_Comment: string;
    function Get_DevInd: TDevIndicator;
    function Get_Initials: string;
    function Get_LabClass: ILabClass;
    function Get_LabCodeId: integer;
    function Get_NormalRange: string;
    function Get_ResultId: integer;
    function Get_SignedAt: TDateTime;
    function Get_SignedByFullName: string;
    function Get_TextResult: string;
    function Get_UnitStr: string;
    function Get_VarName: string;
    function Get_Value: double;
    { Other members }
    property ArithmeticComp: TArithmeticComp read Get_ArithmeticComp;
    property Caption: string read Get_Caption;
    property Comment: string read Get_Comment;
    property DevInd: TDevIndicator read Get_DevInd;
    property Initials: string read Get_Initials;
    property LabClass: ILabClass read Get_LabClass;
    property LabCodeId: integer read Get_LabCodeId;
    property NormalRange: string read Get_NormalRange;
    property ResultId: integer read Get_ResultId;
    property SignedAt: TDateTime read Get_SignedAt;
    property SignedByFullName: string read Get_SignedByFullName;
    property TextResult: string read Get_TextResult;
    property UnitStr: string read Get_UnitStr;
    property VarName: string read Get_VarName;
    property Value: double read Get_Value;
  end;

implementation

uses
  System.SysUtils;

end.
