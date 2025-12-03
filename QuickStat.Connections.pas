unit QuickStat.Connections;

interface

uses
  Classes,
  XmlIntf,
  Generics.Collections;

type
  TQuickStatConnection = class( TObject )
  private
    fName: string;
    fConnectionString: string;
    fStudyName: string;
  public
    procedure Parse( ANode: IXmlNode );
    property Name: string read FName;
    property StudyName: string read FStudyName;
    property ConnectionString: string read FConnectionString;
  end;

  TConnectionList = class( TObjectDictionary<string, TQuickStatConnection> )
  public
    procedure Load( const AFileName: string );
    procedure AddToStrings( AStrings: TStrings );
  end;

implementation

uses
  {General}
  Emetra.Xml.NodeList,
  {Standard}
  SysUtils, XmlDoc;

{ TConnection }

procedure TQuickStatConnection.Parse( ANode: IXmlNode );
begin
  FName := ANode['Name'];
  FStudyName := ANode['StudyName'];
  FConnectionString := ANode['ConnectionString'];
end;

{ TConnectionList }

procedure TConnectionList.AddToStrings( AStrings: TStrings );
var
  connection: TQuickStatConnection;
begin
  for connection in Values do
    AStrings.AddObject( connection.Name, connection );
end;

procedure TConnectionList.Load( const AFileName: string );
var
  connectionNodes: TNodeList;
  newConnection: TQuickStatConnection;
begin
  connectionNodes := TNodeList.Create( LoadXmlDocument( AFileName ), 'Connection' );
  try
    while connectionNodes.Count > 0 do
    begin
      newConnection := TQuickStatConnection.Create;
      newConnection.Parse( connectionNodes[0] );
      connectionNodes.Delete( 0 );
      if ContainsKey( newConnection.Name ) then
        FreeAndNil( newConnection )
      else
        Add( newConnection.Name, newConnection );
    end;
  finally
    FreeAndNil( connectionNodes );
  end;
end;

end.
