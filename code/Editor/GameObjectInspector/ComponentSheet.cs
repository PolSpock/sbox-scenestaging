﻿using Editor.Inspectors;
using Sandbox;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Tools;
namespace Editor.EntityPrefabEditor;

public partial class ComponentSheet : Widget
{
	SerializedObject TargetObject;
	Layout Content;

	internal bool Expanded { get; private set; } = true;

	internal void SetExpanded( bool expanded )
	{
		Expanded = expanded;
		RebuildContent();
	}

	public ComponentSheet( SerializedObject target, Action contextMenu ) : base( null )
	{
		Name = "ComponentSheet";
		TargetObject = target;
		Layout = Layout.Column();
		SetSizeMode( SizeMode.Default, SizeMode.CanShrink );

		var header = Layout.Add( new ComponentHeader( TargetObject, this ) );
		header.MouseRightPress += contextMenu;

		Content = Layout.AddColumn();
		Frame();
	}

	int lastHash;

	[EditorEvent.Frame]
	public void Frame()
	{
		var hash = HashCode.Combine( TargetObject );

		if ( lastHash != hash )
		{
			lastHash = hash;
			RebuildContent();
		}
	}

	[Event.Hotload]
	public void RebuildContent()
	{
		Content.Clear( true );

		BuildInstanceContent();
	}

	void BuildInstanceContent()
	{
		if ( !Expanded ) return;
	
		var props = TargetObject.Where( x => x.HasAttribute<PropertyAttribute>() )
									.OrderBy( x => x.SourceLine )
									.ThenBy( x => x.DisplayName )
									.ToArray();

		var ps = new ControlSheet();
		HashSet<string> handledGroups = new ( StringComparer.OrdinalIgnoreCase );

		foreach( var prop in props )
		{
			if ( !string.IsNullOrWhiteSpace( prop.GroupName ) )
			{
				if ( handledGroups.Contains( prop.GroupName ) )
					continue;

				handledGroups.Add( prop.GroupName );
				AddGroup( ps, prop.GroupName, props.Where( x => x.GroupName == prop.GroupName ).ToArray() );
				continue;
			}

			ps.AddRow( prop );
		}
		
		Content.Add( ps );
	}

	private void AddGroup( ControlSheet sheet, string groupName, SerializedProperty[] props )
	{
		var lo = Layout.Column();
		lo.Spacing = 2;
		var ps = new ControlSheet();

		SerializedProperty skipProperty = null;

		var toggleGroup = props.FirstOrDefault( x => x.HasAttribute<ToggleGroupAttribute>() && x.Name == groupName );
		if ( toggleGroup is not null )
		{
			skipProperty = toggleGroup;

			var label = new Label( groupName );
			label.SetStyles( "color: #ccc; font-weight: bold;" );
			label.FixedHeight = ControlWidget.ControlRowHeight;

			var toggle = ControlWidget.Create( toggleGroup );
			toggle.FixedHeight = 18;
			toggle.FixedWidth = 18;

			var row = Layout.Row();
			row.Spacing = 8;
			row.Add( toggle );
			row.Add( label, 1 );

			lo.Add( row );
		}
		else
		{
			var label = new Label( groupName );
			label.SetStyles( "color: #ccc; font-weight: bold;" );
			label.FixedHeight = ControlWidget.ControlRowHeight;
			lo.Add( label );
		}

		ps.Margin = 0;

		foreach ( var prop in props )
		{
			if ( skipProperty == prop )
				continue;

			ps.AddRow( prop, 8 );
		}

		lo.Add( ps );
		lo.Margin = new Sandbox.UI.Margin( 0, 0, 0, 0 );

		sheet.AddLayout( lo );
	}

	public override void ChildValuesChanged( Widget source )
	{
		//TargetObject.IsChanged();
	}

}

file class ComponentHeader : Widget
{
	SerializedObject TargetObject { get; init; }
	ComponentSheet Sheet { get; set; }

	Layout expanderRect;
	Layout iconRect;
	Layout textRect;

	public ComponentHeader( SerializedObject target, ComponentSheet parent ) : base( parent )
	{
		TargetObject = target;
		Sheet = parent;

		var enabled = ControlWidget.Create( TargetObject.GetProperty( "Enabled" ) );
		enabled.FixedWidth = 18;
		enabled.FixedHeight = 18;

		FixedHeight = 22;

		ContentMargins = 0;
		Layout = Layout.Row();

		expanderRect = Layout.AddRow();
		expanderRect.AddSpacingCell( 22 );

		iconRect = Layout.AddRow();
		iconRect.AddSpacingCell( 22 );

		Layout.AddSpacingCell( 4 );

		// icon

		Layout.Add( enabled );

		Layout.AddSpacingCell( 8 );

		// text 
		textRect = Layout.AddColumn( 1 );
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		float opacity = 0.6f;
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		if ( Paint.HasMouseOver )
		{
			//Paint.ClearPen();
			//Paint.SetBrushRadial( 30, 256, Theme.Blue.WithAlpha( 0.1f ), Theme.Blue.WithAlpha( 0.0f ) ) ;
			//Paint.DrawRect( LocalRect, 0 );
			opacity = 1.0f;
		}

		if ( !Sheet.Expanded )
		{
			Paint.ClearPen();
			Paint.SetBrushRadial( new Vector2( 64, 0 ), 512, Color.Black.WithAlpha( 0.1f ), Color.Black.WithAlpha( 0.0f ) );
			Paint.DrawRect( LocalRect, 0 );
		}
		else
		{
			Paint.ClearPen();
			Paint.SetBrushRadial( new Vector2( 64, 0 ), 256, Theme.Blue.WithAlpha( 0.08f ), Theme.Blue.WithAlpha( 0.03f ) );
			Paint.DrawRect( LocalRect, 0 );

			Paint.SetPen( Theme.Black.WithAlpha( 0.4f ), 2 );
			Paint.DrawLine( LocalRect.BottomLeft, LocalRect.BottomRight );
		}

		Paint.SetPen( Theme.White.WithAlpha( opacity * 0.5f ) );
		Paint.DrawIcon( expanderRect.InnerRect, Sheet.Expanded ? "keyboard_arrow_down" : "chevron_right", 16, TextFlag.Center );

		Paint.SetPen( Theme.Blue.WithAlpha( opacity ) );
		Paint.DrawIcon( iconRect.InnerRect, string.IsNullOrEmpty( TargetObject.TypeIcon ) ? "category" : TargetObject.TypeIcon, 14, TextFlag.Center ); 

		Paint.SetPen( Theme.Blue.Lighten( 0.1f ).WithAlpha( (Sheet.Expanded ? 0.9f : 0.6f) * opacity ) );
		Paint.SetDefaultFont( 8, 1000, false );
		Paint.DrawText( textRect.InnerRect, TargetObject.TypeTitle, TextFlag.LeftCenter );
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		base.OnContextMenu( e );

		var menu = new Menu();
		//menu.AddOption( "Delete Component", "clear", () => Target.Parent.Components.Remove( Target ) );
		menu.OpenAtCursor( false );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		if ( e.LeftMouseButton )
		{
			Sheet.SetExpanded( !Sheet.Expanded );
		}
	}
}
