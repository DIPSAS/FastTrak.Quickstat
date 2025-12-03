unit Emetra.Interfaces.Geography;

interface

type
  IPhysicalAddress = interface
    ['{E47D815B-23E9-4721-BAE0-352DA7648E53}']
    { Property Accessors }
    function Get_City: string;
    function Get_PostCode: string;
    function Get_StreetAddress: string;
    { Properties }
    property City: string read Get_City;
    property PostCode: string read Get_PostCode;
    property StreetAddress: string read Get_StreetAddress;
    { Kith Style naming for the same properties }
    property PostalCode: string read Get_PostCode;
    property StreetAdr: string read Get_StreetAddress;
  end;

  { Alternative names for IPhysicalAddress that may be more descriptive in some situations }

  IGeoAddress = IPhysicalAddress;

  IFolkeregisterAddress = IPhysicalAddress;

  IContactInfo = interface( IPhysicalAddress )
    ['{E47D815B-23E9-4721-BAE0-352DA7648E53}']
    { Property Accessors }
    function Get_Phone: string;
    { Properties }
    property Phone : string read Get_Phone;
  end;

  IStreetAddressDetail = interface['{E9FE221F-C57E-48B9-B900-C45AB34802F7}']
    { Property accessors }
    function Get_StreetName: string;
    function Get_StreetNumber: string;
    { Properties }
    property StreetName: string read Get_StreetName;
    property StreetNumber: string read Get_StreetNumber;
  end;

  IGeoNamedSite = interface( IContactInfo ) ['{DB0118C1-A58C-4630-9F60-51F0878467ED}']
    { Property Accessors }
    function Get_SiteName: string;
    { Properties }
    property SiteName: string read Get_SiteName;
  end;

implementation

end.
