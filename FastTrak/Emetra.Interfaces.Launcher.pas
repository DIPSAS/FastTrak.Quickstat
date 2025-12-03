unit Emetra.Interfaces.Launcher;

interface

type
  ILauncher = interface['{4ED8AE65-9788-4DCC-95F8-B6590CD38FDF}']
    function ExecuteAndWait( const AFileName, AParams: string; AVisibility: integer = 1 ): longword;
  end;

implementation

end.
