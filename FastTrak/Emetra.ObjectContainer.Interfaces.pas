unit Emetra.ObjectContainer.Interfaces;

interface

uses
  System.Classes;

type
  IObjectContainer = interface['{57F263B0-5C29-4AAF-90B1-374FE7FB4EA1}']
    function TryGetObject( const AName: string; out AObject: TObject ): boolean;
    procedure GetObjectNames( ANames: TStrings );
    function GetNamePath: string;
  end;

  IObjectContainerRoot = interface( IObjectContainer ) ['{25F5641B-9D48-41BC-89D7-8FD3500212BC}']
    function GetObject( const APath: string; out AObject: TObject ): boolean;
    procedure RegisterObject( const AName: string; ARestObject: TObject );
    procedure UnregisterObject( const AName: string );
  end;

implementation

end.
