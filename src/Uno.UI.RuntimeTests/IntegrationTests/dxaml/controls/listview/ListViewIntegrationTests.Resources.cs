// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// XAML pages used by ListViewIntegrationTests, copied from
// dxaml/test/resources/res/native/enterprise/ListViewBase.

namespace Microsoft.UI.Xaml.Tests.Enterprise
{
	public partial class ListViewIntegrationTests
	{
		private const string ListViewXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ListView
    x:Name="listView"
    Background="Blue"
    Margin="10"
    Height="380" Width="380">
    <ListView.Resources>
      <x:Double x:Key='ListViewItemMinHeight'>44</x:Double>
    </ListView.Resources>
    <!-- UNO TODO: the default style doesn't see the ListViewItemMinHeight override through ListView.Resources. -->
    <ListView.ItemContainerStyle>
      <Style TargetType="ListViewItem"><Setter Property="MinHeight" Value="44"/></Style>
    </ListView.ItemContainerStyle>
    <ListView.ItemsPanel>
      <ItemsPanelTemplate>
        <ItemsStackPanel CacheLength="6"/>
      </ItemsPanelTemplate>
    </ListView.ItemsPanel>
    <TextBlock Text="Algona"/>
    <TextBlock Text="Auburn"/>
    <TextBlock Text="Beaux Arts Village"/>
    <TextBlock Text="Bellevue"/>
    <TextBlock Text="Black Diamond"/>
    <TextBlock Text="Bothell"/>
    <TextBlock Text="Burien"/>
    <TextBlock Text="Carnation"/>
    <TextBlock Text="Clyde Hill"/>
    <TextBlock Text="Covington"/>
    <TextBlock Text="Des Moines"/>
    <TextBlock Text="Duvall"/>
    <TextBlock Text="Enumclaw"/>
    <TextBlock Text="Federal Way"/>
    <TextBlock Text="Hunts Point"/>
    <TextBlock Text="Issaquah"/>
    <TextBlock Text="Kenmore"/>
    <TextBlock Text="Kent"/>
    <TextBlock Text="Kirkland"/>
    <TextBlock Text="Lake Forest Park"/>
    <TextBlock Text="Maple Valley"/>
    <TextBlock Text="Medina"/>
    <TextBlock Text="Mercer Island"/>
    <TextBlock Text="Milton"/>
    <TextBlock Text="Newcastle"/>
    <TextBlock Text="Normandy Park"/>
    <TextBlock Text="North Bend"/>
    <TextBlock Text="Pacific"/>
    <TextBlock Text="Redmond"/>
    <TextBlock Text="Renton"/>
    <TextBlock Text="Sammamish"/>
    <TextBlock Text="SeaTac"/>
    <TextBlock Text="Seattle"/>
    <TextBlock Text="Shoreline"/>
    <TextBlock Text="Skykomish"/>
    <TextBlock Text="Snoqualmie"/>
    <TextBlock Text="Tukwila"/>
    <TextBlock Text="Woodinville"/>
    <TextBlock Text="Yarrow Point"/>
    <TextBlock Text="Normandy Park"/>
    <TextBlock Text="North Bend"/>
    <TextBlock Text="Pacific"/>
    <TextBlock Text="Redmond"/>
    <TextBlock Text="Renton"/>
    <TextBlock Text="Sammamish"/>
    <TextBlock Text="SeaTac"/>
    <TextBlock Text="Seattle"/>
    <TextBlock Text="Shoreline"/>
    <TextBlock Text="Skykomish"/>
    <TextBlock Text="Snoqualmie"/>
    <TextBlock Text="Tukwila"/>
    <TextBlock Text="Woodinville"/>
    <TextBlock Text="Yarrow Point"/>
  </ListView>
</Page>
""";

		private const string ZoomableListViewXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Grid.Resources>
    <Style x:Key="ListViewStyle1" TargetType="ListView">
      <Setter Property="ScrollViewer.HorizontalScrollBarVisibility" Value="Auto"/>
      <Setter Property="ScrollViewer.VerticalScrollBarVisibility" Value="Auto"/>
      <Setter Property="ScrollViewer.HorizontalScrollMode" Value="Enabled" />
      <Setter Property="ScrollViewer.IsHorizontalRailEnabled" Value="True" />
      <Setter Property="ScrollViewer.VerticalScrollMode" Value="Enabled" />
      <Setter Property="ScrollViewer.IsVerticalRailEnabled" Value="True" />
      <Setter Property="ScrollViewer.ZoomMode" Value="Enabled" />
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="ListView">
            <Border BorderBrush="{TemplateBinding BorderBrush}"
                Background="{TemplateBinding Background}"
                BorderThickness="{TemplateBinding BorderThickness}">
              <ScrollViewer x:Name="ScrollViewer"
                    Padding="{TemplateBinding Padding}"
                    TabNavigation="{TemplateBinding TabNavigation}"
                    HorizontalContentAlignment="Center"
                    VerticalContentAlignment="Center"
                    HorizontalScrollMode="{TemplateBinding ScrollViewer.HorizontalScrollMode}"
                    HorizontalScrollBarVisibility="{TemplateBinding ScrollViewer.HorizontalScrollBarVisibility}"
                    VerticalScrollMode="{TemplateBinding ScrollViewer.VerticalScrollMode}"
                    VerticalScrollBarVisibility="{TemplateBinding ScrollViewer.VerticalScrollBarVisibility}"
                    ZoomMode="{TemplateBinding ScrollViewer.ZoomMode}">
                <ItemsPresenter x:Name="ItemsPresenter" HorizontalAlignment="Center" VerticalAlignment="Center" Margin="50"/>
              </ScrollViewer>
            </Border>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
  </Grid.Resources>

  <ListView x:Name="listView"
    Style="{StaticResource ListViewStyle1}"
    Background="Violet"
    Margin="10" Height="380" Width="380">
    <ListView.Resources>
      <x:Double x:Key='ListViewItemMinHeight'>44</x:Double>
    </ListView.Resources>
    <!-- UNO TODO: the default style doesn't see the ListViewItemMinHeight override through ListView.Resources. -->
    <ListView.ItemContainerStyle>
      <Style TargetType="ListViewItem"><Setter Property="MinHeight" Value="44"/></Style>
    </ListView.ItemContainerStyle>
    <TextBlock Text="Algona"/>
    <TextBlock Text="Auburn"/>
    <TextBlock Text="Beaux Arts Village"/>
    <TextBlock Text="Bellevue"/>
    <TextBlock Text="Black Diamond"/>
    <TextBlock Text="Bothell"/>
    <TextBlock Text="Burien"/>
    <TextBlock Text="Carnation"/>
    <TextBlock Text="Clyde Hill"/>
    <TextBlock Text="Covington"/>
    <TextBlock Text="Des Moines"/>
    <TextBlock Text="Duvall"/>
    <TextBlock Text="Enumclaw"/>
    <TextBlock Text="Federal Way"/>
    <TextBlock Text="Hunts Point"/>
    <TextBlock Text="Issaquah"/>
    <TextBlock Text="Kenmore"/>
    <TextBlock Text="Kent"/>
    <TextBlock Text="Kirkland"/>
    <TextBlock Text="Lake Forest Park"/>
    <TextBlock Text="Maple Valley"/>
    <TextBlock Text="Medina"/>
    <TextBlock Text="Mercer Island"/>
    <TextBlock Text="Milton"/>
    <TextBlock Text="Newcastle"/>
    <TextBlock Text="Normandy Park"/>
    <TextBlock Text="North Bend"/>
    <TextBlock Text="Pacific"/>
    <TextBlock Text="Redmond"/>
    <TextBlock Text="Renton"/>
    <TextBlock Text="Sammamish"/>
    <TextBlock Text="SeaTac"/>
    <TextBlock Text="Seattle"/>
    <TextBlock Text="Shoreline"/>
    <TextBlock Text="Skykomish"/>
    <TextBlock Text="Snoqualmie"/>
    <TextBlock Text="Tukwila"/>
    <TextBlock Text="Woodinville"/>
    <TextBlock Text="Yarrow Point"/>
    <TextBlock Text="Normandy Park"/>
    <TextBlock Text="North Bend"/>
    <TextBlock Text="Pacific"/>
    <TextBlock Text="Redmond"/>
    <TextBlock Text="Renton"/>
    <TextBlock Text="Sammamish"/>
    <TextBlock Text="SeaTac"/>
    <TextBlock Text="Seattle"/>
    <TextBlock Text="Shoreline"/>
    <TextBlock Text="Skykomish"/>
    <TextBlock Text="Snoqualmie"/>
    <TextBlock Text="Tukwila"/>
    <TextBlock Text="Woodinville"/>
    <TextBlock Text="Yarrow Point"/>
  </ListView>
</Grid>
""";

		private const string GroupedListViewXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Grid.Resources>
    <CollectionViewSource x:Name="cvs"/>
    <Style TargetType="TextBox">
      <Setter Property="Padding" Value="10,3,6,5"/>
    </Style>
  </Grid.Resources>

  <ListView x:Name='listView'
    Margin="10" Height="380" Width="380"
    ShowsScrollingPlaceholders='False'
    ItemsSource='{Binding Source={StaticResource cvs}}'>
    <ListView.Resources>
      <x:Double x:Key='ListViewItemMinHeight'>44</x:Double>
    </ListView.Resources>
    <!-- UNO TODO: the default style doesn't see the ListViewItemMinHeight override through ListView.Resources. -->
    <ListView.ItemContainerStyle>
      <Style TargetType="ListViewItem"><Setter Property="MinHeight" Value="44"/></Style>
    </ListView.ItemContainerStyle>
    <ListView.Header>
      <!-- UNO TODO: Height pins the text-measured height of WinUI's Segoe UI, which the expected offsets depend on. -->
      <StackPanel Margin="10" Height="72" Background="Yellow">
        <TextBlock FontSize="15" Text="Header Top"/>
        <TextBox FontSize="15" Text="Header Middle"/>
        <TextBlock FontSize="15" Text="Header Bottom"/>
      </StackPanel>
    </ListView.Header>
    <ListView.Footer>
      <StackPanel Margin="10" Height="72" Background="Orange">
        <TextBlock FontSize="15" Text="Footer Top"/>
        <TextBox FontSize="15" Text="Footer Middle"/>
        <TextBlock FontSize="15" Text="Footer Bottom"/>
      </StackPanel>
    </ListView.Footer>
    <ListView.ItemsPanel>
      <ItemsPanelTemplate>
        <ItemsStackPanel CacheLength='6' AreStickyGroupHeadersEnabled='True'/>
      </ItemsPanelTemplate>
    </ListView.ItemsPanel>
    <ListView.ItemTemplate>
      <DataTemplate>
        <TextBlock FontSize='15' Text='{Binding}' />
      </DataTemplate>
    </ListView.ItemTemplate>
    <ListView.GroupStyle>
      <GroupStyle>
        <GroupStyle.HeaderTemplate>
          <DataTemplate>
            <Grid Background='Blue'>
              <TextBlock Text='{Binding}' Foreground='White' FontSize='17' />
            </Grid>
          </DataTemplate>
        </GroupStyle.HeaderTemplate>
      </GroupStyle>
    </ListView.GroupStyle>
  </ListView>
</Grid>
""";

		private const string ListViewWithDraggableItemsXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Page.Resources>
    <Style x:Key="DefaultListViewItemContainerStyle" TargetType="ListViewItem">
      <Setter Property="HorizontalContentAlignment" Value="Stretch" />
      <Setter Property="Margin" Value="0" />
      <Setter Property="Padding" Value="0" />
    </Style>

    <Style x:Key="CompactBasicListViewStyle" TargetType="ListView">
      <Setter Property="ItemContainerStyle" Value="{StaticResource DefaultListViewItemContainerStyle}" />
    </Style>
  </Page.Resources>

  <ListView 
    x:Name="listView"
    Style="{StaticResource CompactBasicListViewStyle}"
    CanDragItems="True"
    SelectionMode="None">
    <ListView.ItemTemplate>
      <DataTemplate>
        <Border Background="Orange" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Height="44" Margin="2">
          <TextBlock Text="{Binding}" VerticalAlignment="Center"/>
        </Border>
      </DataTemplate>
    </ListView.ItemTemplate>
  </ListView>
</Page>
""";

		private const string ListViewForTabNavigationXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <Button
      x:Name="BeforeButton"
      Content="Before Button"
      Tag="B"/>

    <StackPanel Grid.Row="1">
      <ListView x:Name="MainListView" Visibility="Collapsed">
        <ListView.Header>
          <Button Visibility="Collapsed" Tag="H" Content="MainListView Header"/>
        </ListView.Header>
        <ListView.Footer>
          <Button Visibility="Collapsed" Tag="F" Content="MainListView Footer"/>
        </ListView.Footer>
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <StackPanel>
              <Button Content="{Binding}" Tag="{Binding}"/>
              <ListView SelectionMode="None">
                <ListView.ItemsPanel>
                  <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal"/>
                  </ItemsPanelTemplate>
                </ListView.ItemsPanel>
                <ListView.ItemContainerStyle>
                  <Style TargetType="ListViewItem">
                    <Setter Property="IsTabStop" Value="False"/>
                  </Style>
                </ListView.ItemContainerStyle>
                <ListView.ItemTemplate>
                  <DataTemplate>
                    <Button Content="{Binding}" Tag="{Binding}"/>
                  </DataTemplate>
                </ListView.ItemTemplate>
                <x:String>.1</x:String>
                <x:String>.2</x:String>
                <x:String>.3</x:String>
              </ListView>
            </StackPanel>
          </DataTemplate>
        </ListView.ItemTemplate>
        <x:String>1</x:String>
        <x:String>2</x:String>
        <x:String>3</x:String>
      </ListView>
      
      <ListView x:Name="MainVirtualizedListView" SelectionMode="None" Visibility="Collapsed" TabNavigation="Local" MaxHeight="250">
        <ListView.ItemsPanel>
          <ItemsPanelTemplate>
            <ItemsStackPanel CacheLength="1"/>
          </ItemsPanelTemplate>
        </ListView.ItemsPanel>
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <StackPanel>
              <Button Content="{Binding}" Tag="{Binding}"/>
              <ListView SelectionMode="None" SingleSelectionFollowsFocus="False">
                <ListView.ItemsPanel>
                  <ItemsPanelTemplate>
                    <ItemsWrapGrid
                        HorizontalAlignment="Stretch"
                        VerticalAlignment="Stretch"
                        MaximumRowsOrColumns="1"
                        Orientation="Vertical"/>
                  </ItemsPanelTemplate>
                </ListView.ItemsPanel>
                <ListView.ItemContainerStyle>
                  <Style TargetType="ListViewItem">
                    <Setter Property="IsTabStop" Value="False"/>
                  </Style>
                </ListView.ItemContainerStyle>
                <ListView.ItemTemplate>
                  <DataTemplate>
                    <Button Content="{Binding}" Tag="{Binding}"/>
                  </DataTemplate>
                </ListView.ItemTemplate>
                <x:String>.1</x:String>
                <x:String>.2</x:String>
                <x:String>.3</x:String>
              </ListView>
            </StackPanel>
          </DataTemplate>
        </ListView.ItemTemplate>
        <x:String>1</x:String>
        <x:String>2</x:String>
        <x:String>3</x:String>
        <x:String>4</x:String>
        <x:String>5</x:String>
        <x:String>6</x:String>
        <x:String>7</x:String>
        <x:String>8</x:String>
        <x:String>9</x:String>
        <x:String>10</x:String>
        <x:String>11</x:String>
        <x:String>12</x:String>
        <x:String>13</x:String>
        <x:String>14</x:String>
        <x:String>15</x:String>
      </ListView>

      <ListView x:Name="MainListViewWithVirtualizingStackPanel" Visibility="Collapsed">
        <ListView.Header>
          <Button Visibility="Collapsed" Tag="H" Content="MainListView Header"/>
        </ListView.Header>
        <ListView.Footer>
          <Button Visibility="Collapsed" Tag="F" Content="MainListView Footer"/>
        </ListView.Footer>
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <StackPanel>
              <Button Content="{Binding}" Tag="{Binding}"/>
              <ListView SelectionMode="None">
                <ListView.ItemsPanel>
                  <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal"/>
                  </ItemsPanelTemplate>
                </ListView.ItemsPanel>
                <ListView.ItemContainerStyle>
                  <Style TargetType="ListViewItem">
                    <Setter Property="IsTabStop" Value="False"/>
                  </Style>
                </ListView.ItemContainerStyle>
                <ListView.ItemTemplate>
                  <DataTemplate>
                    <Button Content="{Binding}" Tag="{Binding}"/>
                  </DataTemplate>
                </ListView.ItemTemplate>
                <x:String>.1</x:String>
                <x:String>.2</x:String>
                <x:String>.3</x:String>
              </ListView>
            </StackPanel>
          </DataTemplate>
        </ListView.ItemTemplate>
        <ListView.ItemsPanel>
          <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
          </ItemsPanelTemplate>
        </ListView.ItemsPanel>
        <x:String>1</x:String>
        <x:String>2</x:String>
        <x:String>3</x:String>
      </ListView>

      <ListView x:Name="MainListViewWithStackPanel" Visibility="Collapsed">
        <ListView.Header>
          <Button Visibility="Collapsed" Tag="H" Content="MainListView Header"/>
        </ListView.Header>
        <ListView.Footer>
          <Button Visibility="Collapsed" Tag="F" Content="MainListView Footer"/>
        </ListView.Footer>
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <StackPanel>
              <Button Content="{Binding}" Tag="{Binding}"/>
              <ListView SelectionMode="None">
                <ListView.ItemsPanel>
                  <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal"/>
                  </ItemsPanelTemplate>
                </ListView.ItemsPanel>
                <ListView.ItemContainerStyle>
                  <Style TargetType="ListViewItem">
                    <Setter Property="IsTabStop" Value="False"/>
                  </Style>
                </ListView.ItemContainerStyle>
                <ListView.ItemTemplate>
                  <DataTemplate>
                    <Button Content="{Binding}" Tag="{Binding}"/>
                  </DataTemplate>
                </ListView.ItemTemplate>
                <x:String>.1</x:String>
                <x:String>.2</x:String>
                <x:String>.3</x:String>
              </ListView>
            </StackPanel>
          </DataTemplate>
        </ListView.ItemTemplate>
        <ListView.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel/>
          </ItemsPanelTemplate>
        </ListView.ItemsPanel>
        <x:String>1</x:String>
        <x:String>2</x:String>
        <x:String>3</x:String>
      </ListView>

      <ListView x:Name="MainListViewWithNestedButtons" SelectionMode="None" Visibility="Collapsed" TabNavigation="Local" MaxHeight="250">
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <Grid RowDefinitions="Auto, Auto">
              <TextBlock Text="{Binding}"/>
              <GridView
                  Grid.Row="1"
                  SelectionMode="None"
                  SingleSelectionFollowsFocus="False"
                  HorizontalAlignment="Stretch"
                  VerticalAlignment="Stretch"
                  ScrollViewer.HorizontalScrollBarVisibility="Auto"
                  ScrollViewer.HorizontalScrollMode="Auto"
                  ScrollViewer.VerticalScrollBarVisibility="Disabled"
                  ScrollViewer.VerticalScrollMode="Disabled">
                <GridView.ItemsPanel>
                  <ItemsPanelTemplate>
                    <ItemsWrapGrid
                        HorizontalAlignment="Stretch"
                        VerticalAlignment="Stretch"
                        MaximumRowsOrColumns="1"
                        Orientation="Vertical"/>
                  </ItemsPanelTemplate>
                </GridView.ItemsPanel>
                <GridView.ItemTemplate>
                  <DataTemplate>
                    <Grid Width="80" Height="50">
                      <TextBlock
                          HorizontalTextAlignment="Center"
                          VerticalAlignment="Top"
                          Text="{Binding}"/>
                      <Button
                          HorizontalAlignment="Center"
                          VerticalAlignment="Bottom"
                          Content="{Binding}" Tag="{Binding}"/>
                    </Grid>
                  </DataTemplate>
                </GridView.ItemTemplate>

                <x:String>one</x:String>
                <x:String>two</x:String>
                <x:String>three</x:String>
              </GridView>
            </Grid>
          </DataTemplate>
        </ListView.ItemTemplate>

        <x:String>First Nested GridView</x:String>
        <x:String>Second Nested GridView</x:String>
        <x:String>Third Nested GridView</x:String>
      </ListView>
    </StackPanel>

    <Button
      x:Name="AfterButton"
      Content="After Button"
      Tag="A"
      Grid.Row="2"/>
  </Grid>
</Page>
""";

		private const string GroupedListViewForTabNavigationXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <Grid.Resources>
      <CollectionViewSource x:Name="cvs"/>
    </Grid.Resources>

    <Button
      x:Name="BeforeButton"
      Content="Before Button"
      Tag="B"/>

    <StackPanel Grid.Row="1">
      <ListView x:Name="MainListView"
        Visibility="Collapsed"
        ItemsSource='{Binding Source={StaticResource cvs}}'>
        <ListView.Header>
          <Button Visibility="Collapsed" Tag="H" Content="MainListView Header"/>
        </ListView.Header>
        <ListView.Footer>
          <Button Visibility="Collapsed" Tag="F" Content="MainListView Footer"/>
        </ListView.Footer>
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <Button Content="{Binding}" Tag="{Binding}"/>
          </DataTemplate>
        </ListView.ItemTemplate>
        <ListView.GroupStyle>
          <GroupStyle>
            <GroupStyle.HeaderTemplate>
              <DataTemplate>
                <TextBlock Text="{Binding}" Foreground="Red"/>
              </DataTemplate>
            </GroupStyle.HeaderTemplate>
          </GroupStyle>
        </ListView.GroupStyle>
      </ListView>

      <ListView x:Name="MainListViewWithVirtualizingStackPanel"
        Visibility="Collapsed"
        ItemsSource='{Binding Source={StaticResource cvs}}'>
        <ListView.Header>
          <Button Visibility="Collapsed" Tag="H" Content="MainListView Header"/>
        </ListView.Header>
        <ListView.Footer>
          <Button Visibility="Collapsed" Tag="F" Content="MainListView Footer"/>
        </ListView.Footer>
        <ListView.ItemContainerStyle>
          <Style TargetType="ListViewItem">
            <Setter Property="IsTabStop" Value="False"/>
          </Style>
        </ListView.ItemContainerStyle>
        <ListView.ItemTemplate>
          <DataTemplate>
            <Button Content="{Binding}" Tag="{Binding}"/>
          </DataTemplate>
        </ListView.ItemTemplate>
        <ListView.GroupStyle>
          <GroupStyle>
            <GroupStyle.HeaderTemplate>
              <DataTemplate>
                <TextBlock Text="{Binding}" Foreground="Red"/>
              </DataTemplate>
            </GroupStyle.HeaderTemplate>
          </GroupStyle>
        </ListView.GroupStyle>
        <ListView.ItemsPanel>
          <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
          </ItemsPanelTemplate>
        </ListView.ItemsPanel>
      </ListView>
    </StackPanel>

    <Button
      x:Name="AfterButton"
      Content="After Button"
      Tag="A"
      Grid.Row="2"/>
  </Grid>
</Page>
""";

		private const string NestedListViewBaseXaml = """
<!-- Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information. -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ListView x:Name="outerListView" SelectionMode="None" TabNavigation="Local" MaxHeight="400">
    <!--<ListView.ItemsPanel>
      <ItemsPanelTemplate>
        <ItemsStackPanel CacheLength="2"/>
      </ItemsPanelTemplate>
    </ListView.ItemsPanel>-->
    <ListView.ItemContainerStyle>
      <Style TargetType="ListViewItem">
        <Setter Property="IsTabStop" Value="False"/>
        <Setter Property="Padding" Value="2"/>
        <Setter Property="Margin" Value="2"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="White"/>
      </Style>
    </ListView.ItemContainerStyle>
    <ListView.ItemTemplate>
      <DataTemplate>
        <Grid Margin="0,0,0,10" RowSpacing="10" RowDefinitions="Auto, Auto">
          <TextBlock Text="{Binding}" FontSize="14" Margin="1"/>
          <GridView
              Grid.Row="1"
              Margin="1"
              SelectionMode="None"
              SingleSelectionFollowsFocus="False"
              HorizontalAlignment="Stretch"
              VerticalAlignment="Stretch"
              ScrollViewer.HorizontalScrollBarVisibility="Auto"
              ScrollViewer.HorizontalScrollMode="Auto"
              ScrollViewer.VerticalScrollBarVisibility="Disabled"
              ScrollViewer.VerticalScrollMode="Disabled">
            <GridView.ItemsPanel>
              <ItemsPanelTemplate>
                <ItemsWrapGrid
                    HorizontalAlignment="Stretch"
                    VerticalAlignment="Stretch"
                    MaximumRowsOrColumns="1"
                    Orientation="Vertical"/>
              </ItemsPanelTemplate>
            </GridView.ItemsPanel>
            <GridView.ItemTemplate>
              <DataTemplate>
                <Grid Width="100" Height="100" Background="White">
                  <TextBlock
                      VerticalAlignment="Center"
                      HorizontalTextAlignment="Center"
                      Text="{Binding}"/>
                </Grid>
              </DataTemplate>
            </GridView.ItemTemplate>

            <x:String>one</x:String>
            <x:String>two</x:String>
            <x:String>three</x:String>
          </GridView>
        </Grid>
      </DataTemplate>
    </ListView.ItemTemplate>

    <x:String>First Nested GridView</x:String>
    <x:String>Second Nested GridView</x:String>
    <x:String>Third Nested GridView</x:String>
    <x:String>Fourth Nested GridView</x:String>
    <x:String>Fifth Nested GridView</x:String>
    <x:String>Sixth Nested GridView</x:String>
    <x:String>Seventh Nested GridView</x:String>
    <x:String>Eighth Nested GridView</x:String>
    <x:String>Ninth Nested GridView</x:String>
    <x:String>Tenth Nested GridView</x:String>
  </ListView>
</Page>
""";

	}
}
