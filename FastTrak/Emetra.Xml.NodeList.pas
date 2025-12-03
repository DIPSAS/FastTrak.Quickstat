unit Emetra.Xml.NodeList;

interface

uses
  Generics.Collections, XmlIntf;

type
  TNodeList = class( TList<IXmlNode> )
  public
    constructor Create( const AStartAt: IXmlNode; const ANodeToAdd: string ); overload;
    constructor Create( const AXml: IXmlDocument; const ANodeToAdd: string ); overload;
    { other methods }
    function FindNodes( AStartAt: IXmlNode; const ANodeName: string ): integer;
  end;

implementation

{ TNodeList }

constructor TNodeList.Create( const AStartAt: IXmlNode; const ANodeToAdd: string );
begin
  inherited Create;
  FindNodes( AStartAt, ANodeToAdd );
end;

constructor TNodeList.Create(const AXml: IXmlDocument; const ANodeToAdd: string);
begin
  inherited Create;
  FindNodes( AXml.DocumentElement, ANodeToAdd );
end;

function TNodeList.FindNodes( AStartAt: IXmlNode; const ANodeName: string ): integer;
var
  n: integer;
  thisNode: IXmlNode;
begin
  if AStartAt.HasChildNodes then
  begin
    n := 0;
    while n < AStartAt.ChildNodes.Count do
    begin
      thisNode := AStartAt.ChildNodes[n];
      if thisNode.NodeName = ANodeName then
        Self.Add( thisNode );
      FindNodes( thisNode, ANodeName );
      inc( n );
    end;
  end;
  Result := Count;
end;

end.
