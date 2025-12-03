unit Emetra.Interfaces.SaveLoad;

interface

uses
  Db;

type
  ILoad = interface(IInterface)
    ['{8E552BB9-C6D5-462F-9C57-7CEE317A73AB}']
    procedure Load(ADataset: TDataset);
  end;

  ISave = interface(IInterface)
    ['{2CAAEBEC-AA9C-4D79-814F-DE417E4E64FE}']
    function Save: boolean;
    function Saved: boolean;
  end;

implementation

end.
