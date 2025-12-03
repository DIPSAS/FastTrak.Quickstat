unit Emetra.Database.ParameterDictionary.Interfaces;

interface

uses
  Data.Db;

type
  IParameterDictionary = interface
    ['{F09EAE0F-99CD-49DF-965A-81157F5DAC44}']
    function TryApplyParameters( const AQuery: string; AParams: TParams ): boolean;
  end;

implementation

end.
