unit Emetra.ObjectContainer;

interface

uses
  { General classes }
  Emetra.Classes.Business,
  Emetra.Classes.Tokenizer,
  { General interfaces }
  Emetra.ObjectContainer.Interfaces,
  { Standard }
  Generics.Collections, SysUtils, Rtti, System.TypInfo, System.Classes;

type
  ERestException = class( Exception );
  ERestInvalidPathException = class( ERestException );
  ERestUnknownPropertyException = class( ERestException );

  TContainedObjects = TDictionary<string,TObject>;

  TObjectContainer = class( TBusiness, IObjectContainerRoot, IObjectContainer )
  private
    FObjectPath: TTokenizer;
    FPropValue: TTokenizer;
    FPreferStrings: boolean;
    FPropertyName: string;
    FRttiContext: TRttiContext;
    function GetObject( const APath: string; out AObject: TObject ): boolean;
    function Get_Count: integer;
  protected
    FDictionary: TContainedObjects;
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    function TryGetObject( const AName: string; out AObject: TObject ): boolean;
    procedure RegisterObject( const AName: string;  AObject: TObject );
    procedure UnregisterObject( const AName: string );
    procedure GetObjectNames( AStrings: TStrings );
  published
    property Count: integer read Get_Count;
  end;

implementation

uses
  { General }
  Emetra.Classes.Auditing,
  { Standard }
  Variants;

const
  MSG_NOT_CONTAINER = 'No, %s is not a container, so child "%s" can not exist unless it is a property.';

{$IFDEF Debug}

  WARN_UNKNOWN_CHILD = 'No, %s is a Container, but has no Child "%s"';
  MSG_CHILD_FOUND = 'Yes, %s is a Container, and has Child of class "%s" named %s';

{$ENDIF}

{ TObjectContainer }

procedure TObjectContainer.AfterConstruction;
begin
  inherited;
  FPreferStrings := true;
  FObjectPath := TTokenizer.Create;
  FPropValue:= TTokenizer.Create;
  FDictionary := TContainedObjects.Create;
  FRttiContext := TRttiContext.Create;
end;

procedure TObjectContainer.BeforeDestruction;
begin
  { FRttiContext is a record, no need to free it }
  FDictionary.Clear;
  SafeFree( FDictionary );
  SafeFree( FPropValue );
  SafeFree( FObjectPath );
  inherited;
end;

function TObjectContainer.GetObject(const APath: string; out AObject: TObject): boolean;
var
  restContainer: IObjectContainer;
  parentObject: TObject;
  childName: string;
begin
  AObject := nil;
  parentObject := Self;
  FPropertyName := '';
  FObjectPath.Prepare( APath, '/' );
  if FObjectPath.Count>0 then
  repeat
    FPropValue.Prepare( FObjectPath[0], '.' );
    if Supports( parentObject, IObjectContainer, restContainer ) then
    begin
      childName := FPropValue[0];
      Log.SilentSuccess( 'Supports(%s,IObjectContainer)!', [parentObject.ClassName] );
      if restContainer.TryGetObject( childName, AObject ) then
      begin
        {$IFDEF Debug}
        Log.SilentSuccess( MSG_CHILD_FOUND, [parentObject.ClassName,AObject.ClassName,FObjectPath[0]] );
        {$ENDIF}
      end
      else
      begin
        {$IFDEF Debug}
        Log.SilentWarning( WARN_UNKNOWN_CHILD, [parentObject.ClassName,childName] );
        {$ENDIF}
        AObject := nil;
        break;
      end;
    end
    else
      Log.Event( MSG_NOT_CONTAINER, [parentObject.ClassName,childName] );
    if Assigned( AObject ) and ( FObjectPath.Count = 1 ) then
    begin
      FPropertyName := FPropValue[1];
      break;
    end;
    FObjectPath.Delete( 0 );
    parentObject := AObject;
  until FObjectPath.Count = 0;
  Result := Assigned( AObject );
end;

procedure TObjectContainer.GetObjectNames(AStrings: TStrings);
var
  objectName: string;
begin
  for objectName in FDictionary.Keys do
    AStrings.Add( objectName );
end;

function TObjectContainer.Get_Count: integer;
begin
  Result := FDictionary.Count;
end;

function TObjectContainer.TryGetObject(const AName: string; out AObject: TObject): boolean;
begin
  AObject := nil;
  Result := FDictionary.TryGetValue( AnsiUppercase( AName ), AObject );
end;

procedure TObjectContainer.RegisterObject(const AName: string; AObject: TObject );
begin
  FDictionary.AddOrSetValue( AnsiUppercase( AName ), AObject );
end;

procedure TObjectContainer.UnregisterObject(const AName: string);
begin
  FDictionary.Remove( AnsiUppercase( AName ) );
end;

end.

