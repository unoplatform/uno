using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Verify = Uno.UI.SourceGenerators.Tests.Verifiers.XamlSourceGeneratorVerifier;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

public partial class Given_Parser
{
	[TestMethod]
	public async Task When_InvalidXaml_Case001()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage" Background="{StaticResource BackgroundColor}" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:sys="using:System">
				
				  <Grid>
				    <Grid.RowDefinitions>
				      <RowDefinition Height="64" />
				      <RowDefinition Height="*" />
				      <RowDefinition Height="48" />
				    </Grid.RowDefinitions>
				    <Grid.ColumnDefinitions>
				      <ColumnDefinition Width="*" />
				      <ColumnDefinition Width="320" />
				    </Grid.ColumnDefinitions>
				
				    <Grid Grid.Row="0" Grid.ColumnSpan="2" Background="{StaticResource PrimaryColor}" Padding="16,16,16,16" VerticalAlignment="Center">
				      <Grid.ColumnDefinitions>
				        <ColumnDefinition Width="*" />
				        <ColumnDefinition Width="Auto" />
				      </Grid.ColumnDefinitions>
				
				      <StackPanel Orientation="Vertical">
				        <TextBlock x:Uid="EmptyPage.Header.Title" Text="{Binding Repo.Name}" Style="{StaticResource MaterialTitleLarge}" Foreground="{StaticResource OnPrimaryColor}" />
				        <TextBlock x:Uid="EmptyPage.Header.Subtitle" Text="{Binding Repo.Description}" Style="{StaticResource MaterialBodySmall}" Foreground="{StaticResource OnPrimaryColor}" Opacity="0.9" />
				      </StackPanel>
				
				      <StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center" Spacing="12">
				        <Button x:Uid="EmptyPage.Header.Open" Style="{StaticResource MaterialFilledButtonStyle}" Command="{Binding OpenRepoCommand}" MinHeight="40" MinWidth="120" HorizontalAlignment="Right">
				          <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="8">
				            <SymbolIcon Symbol="OpenLocal" />
				            <TextBlock Text="Open on GitHub" />
				          </StackPanel>
				        </Button>
				      </StackPanel>
				    </Grid>
				
				    <ScrollViewer Grid.Row="1" Grid.Column="0" Padding="16,16,16,16" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
				      <StackPanel Orientation="Vertical" Spacing="16" Padding="0,0,0,0">
				          <StackPanel Orientation="Vertical" Spacing="8">
				            <TextBlock Text="{Binding Repo.Name}" Style="{StaticResource MaterialTitleMedium}" />
				            <TextBlock Text="{Binding Repo.Description}" Style="{StaticResource MaterialBodyMedium}" TextWrapping="Wrap" />
				            <StackPanel Orientation="Horizontal" Spacing="8" Justify="SpaceBetween" CounterAxisAlignment="Center">
				              <StackPanel Orientation="Horizontal" Spacing="8">
				                <Button x:Uid="EmptyPage.Hero.Clone" Style="{StaticResource MaterialOutlinedButtonStyle}" Command="{Binding CloneCommand}" MinHeight="40">
				                  <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Download" /><TextBlock Text="Clone" /></StackPanel>
				                </Button>
				                <Button x:Uid="EmptyPage.Hero.Star" Style="{StaticResource MaterialOutlinedButtonStyle}" Command="{Binding StarCommand}" MinHeight="40">
				                  <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Favorite" /><TextBlock Text="Star" /></StackPanel>
				                </Button>
				              </StackPanel>
				              <StackPanel Orientation="Horizontal" Spacing="8">
				                <Button x:Uid="EmptyPage.Hero.Refresh" Style="{StaticResource MaterialFilledButtonStyle}" Command="{Binding RefreshCommand}" MinHeight="40">
				                  <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Sync" /><TextBlock Text="Refresh" /></StackPanel>
				                </Button>
				              </StackPanel>
				            </StackPanel>
				          </StackPanel>
				
				        <ItemsControl ItemsSource="{Binding Metrics}">
				          <ItemsControl.ItemsPanel>
				            <ItemsPanelTemplate>
				              <StackPanel Orientation="Horizontal" Spacing="16" CounterAxisAlignment="Stretch" />
				            </ItemsPanelTemplate>
				          </ItemsControl.ItemsPanel>
				          <ItemsControl.ItemTemplate>
				            <DataTemplate>
				                <StackPanel Orientation="Vertical" Spacing="8">
				                  <TextBlock Text="{Binding Title}" Style="{StaticResource MaterialLabelLarge}" />
				                  <TextBlock Text="{Binding Value}" Style="{StaticResource MaterialTitleLarge}" />
				                  <TextBlock>
				                    <Run Text="{Binding Delta}" />
				                    <Run Text=" " />
				                    <Run Text="{Binding DeltaLabel}" />
				                  </TextBlock>
				                </StackPanel>
				            </DataTemplate>
				          </ItemsControl.ItemTemplate>
				        </ItemsControl>
				
				        <TextBlock Text="Recent Activity" Style="{StaticResource MaterialTitleSmall}" Margin="0,8,0,0" />
				        <ListView ItemsSource="{Binding ActivityItems}" Style="{StaticResource MaterialListViewStyle}" IsItemClickEnabled="True">
				          <ListView.ItemTemplate>
				            <DataTemplate>
				                <Grid>
				                  <Grid.ColumnDefinitions>
				                    <ColumnDefinition Width="*" />
				                    <ColumnDefinition Width="Auto" />
				                  </Grid.ColumnDefinitions>
				                  <StackPanel Orientation="Vertical">
				                    <TextBlock Text="{Binding Title}" Style="{StaticResource MaterialBodyMedium}" TextWrapping="Wrap" />
				                    <TextBlock Text="{Binding Subtitle}" Style="{StaticResource MaterialCaptionSmall}" Opacity="0.8" />
				                  </StackPanel>
				                  <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
				                    <Button Style="{StaticResource MaterialTextButtonStyle}" Command="{Binding OpenItemCommand RelativeSource={RelativeSource AncestorType=Page}}" CommandParameter="{Binding}" MinHeight="36">
				                      <SymbolIcon Symbol="OpenLocal" />
				                    </Button>
				                    <Button Style="{StaticResource MaterialTextButtonStyle}" Command="{Binding CommentCommand RelativeSource={RelativeSource AncestorType=Page}}" CommandParameter="{Binding}" MinHeight="36">
				                      <SymbolIcon Symbol="Comment" />
				                    </Button>
				                  </StackPanel>
				                </Grid>
				            </DataTemplate>
				          </ListView.ItemTemplate>
				        </ListView>
				      </StackPanel>
				    </ScrollViewer>
				
				    <Border Grid.Row="1" Grid.Column="1" Background="{StaticResource SurfaceVariantColor}" Padding="16,16,16,16">
				      <StackPanel Orientation="Vertical" Spacing="12" CounterAxisAlignment="Stretch">
				        <TextBlock Text="Actions" Style="{StaticResource MaterialTitleSmall}" />
				        <Button x:Uid="EmptyPage.Rail.NewIssue" Style="{StaticResource MaterialFilledButtonStyle}" Command="{Binding NewIssueCommand}" MinHeight="44">
				          <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Add" /><TextBlock Text="New Issue" /></StackPanel>
				        </Button>
				        <Button x:Uid="EmptyPage.Rail.NewPR" Style="{StaticResource MaterialOutlinedButtonStyle}" Command="{Binding NewPRCommand}" MinHeight="44">
				          <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Add" /><TextBlock Text="New PR" /></StackPanel>
				        </Button>
				        <Button x:Uid="EmptyPage.Rail.Merge" Style="{StaticResource MaterialFilledButtonStyle}" Command="{Binding MergeCommand}" MinHeight="44">
				          <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Accept" /><TextBlock Text="Merge" /></StackPanel>
				        </Button>
				        <Button x:Uid="EmptyPage.Rail.Deploy" Style="{StaticResource MaterialOutlinedButtonStyle}" Command="{Binding DeployCommand}" MinHeight="44">
				          <StackPanel Orientation="Horizontal" Spacing="8"><SymbolIcon Symbol="Upload" /><TextBlock Text="Deploy" /></StackPanel>
				        </Button>
				
				          <StackPanel Orientation="Vertical" Spacing="6">
				            <TextBlock Text="CI Status" Style="{StaticResource MaterialLabelLarge}" />
				            <TextBlock Text="{Binding CI.Status}" Style="{StaticResource MaterialBodyMedium}" />
				            <ProgressBar Value="{Binding CI.Progress}" Maximum="100" Height="6" Style="{StaticResource MaterialProgressBarStyle}" />
				          </StackPanel>
				
				        <TextBlock Text="Filters" Style="{StaticResource MaterialLabelSmall}" />
				        <StackPanel Orientation="Vertical" Spacing="8">
				          <CheckBox Content="Show open only" IsChecked="{Binding Filters.OpenOnly}" />
				          <CheckBox Content="Show PRs" IsChecked="{Binding Filters.IncludePRs}" />
				        </StackPanel>
				      </StackPanel>
				    </Border>
				
				    <Grid Grid.Row="2" Grid.ColumnSpan="2" Background="{StaticResource SurfaceColor}" Padding="16,16,16,16">
				      <TextBlock Text="{Binding Repo.Footnote}" Style="{StaticResource MaterialCaptionSmall}" VerticalAlignment="Center" />
				    </Grid>
				  </Grid>
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 40, 14, 40, 14).WithArguments("Property 'CounterAxisAlignment' does not exist on 'StackPanel'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 40, 14, 40, 14).WithArguments("Property 'Justify' does not exist on 'StackPanel'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 60, 16, 60, 16).WithArguments("Property 'CounterAxisAlignment' does not exist on 'StackPanel'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 107, 8, 107, 8).WithArguments("Property 'CounterAxisAlignment' does not exist on 'StackPanel'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 92, 37, 92, 37).WithArguments("Missing comma between members of 'Binding'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 92, 22, 92, 22).WithArguments("Property 'AncestorType' is not supported on 'RelativeSource'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 95, 36, 95, 36).WithArguments("Missing comma between members of 'Binding'"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 95, 22, 95, 22).WithArguments("Property 'AncestorType' is not supported on 'RelativeSource'"),
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_InvalidXaml_Case001_BindingError()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page
					x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<TextBlock Text="{Binding ThePath RelativeSource={RelativeSource TemplatedParent}}" />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 7, 10, 7, 10).WithArguments("Missing comma between members of 'Binding'"),
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_InvalidXaml_Case001_InvalidResourceSourceProperty()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page
					x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<TextBlock Text="{Binding ThePath, RelativeSource={RelativeSource AncestorType=Page}}" />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 7, 3, 7, 3).WithArguments("Property 'AncestorType' is not supported on 'RelativeSource'"),
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_InvalidXaml_Case001_InvalidResourceSourceMode()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page
					x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<TextBlock Text="{Binding ThePath, RelativeSource={RelativeSource Mode=Page}}" />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 7, 3, 7, 3).WithArguments("'Page' is not a valid 'RelativeSourceMode'"),
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_InvalidXaml_Case002()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage" Background="{StaticResource BackgroundColor}" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:sys="using:System">
				
				  	<Grid Margin="16,16,16,16">
				<Grid Grid.Row="0">
				<Image HorizontalAlignment="Left" VerticalAlignment="Center" Height="40" Width="40" Source="ms-appx:///Assets/Icons/iconLogo.targetsize-32.png" Grid.Column="0" x:Uid="MainPage.Image.Logo" />
				    <TextBlock Text="Activités GitHub" Style="{StaticResource TitleTextBlockStyle}" VerticalAlignment="Center" Grid.Column="1" x:Uid="MainPage.Label.Title" />
				    <StackPanel HorizontalAlignment="Right" VerticalAlignment="Center" Orientation="Horizontal" Grid.Column="2">
				    <Button Margin="0,0,8,0" Style="{StaticResource DefaultButtonStyle}" Command="{Binding SyncCommand}" Content="Sync" x:Uid="MainPage.Button.Sync" />
				        <Button Style="{StaticResource AccentButtonStyle}" Command="{Binding PushToBambooCommand}" Content="Push sélection" x:Uid="MainPage.Button.Push" />
				    </StackPanel>
				    <Grid.ColumnDefinitions>
				    <ColumnDefinition Width="56" />
				    <ColumnDefinition Width="*" />
				        <ColumnDefinition Width="Auto" />
				    </Grid.ColumnDefinitions>
				</Grid>
				<Grid Margin="0,12,0,0" Grid.Row="1">
				<ListView ItemContainerStyle="{StaticResource DefaultListViewItemStyle}" SelectedItem="{Binding SelectedActivity}" ItemsSource="{Binding Activities}" Grid.Column="0" x:Uid="MainPage.List.Activities" x:Name="ActivitiesList">
				<ListView.ItemTemplate><DataTemplate>
				    <Border BorderThickness="1,1,1,1" Margin="0,6,0,6" Padding="12,12,12,12" CornerRadius="8,8,8,8">
				    <Grid>
				        <Border VerticalAlignment="Top" CornerRadius="20,20,20,20" Height="40" Width="40">
				        <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Source="{Binding SourceIconPath}" /></Border>
				        <StackPanel Margin="12,0,12,0" Grid.Column="1">
				        <StackPanel Orientation="Horizontal">
				        <TextBlock Style="{StaticResource BodyStrongTextBlockStyle}">
				        <Run Text="{Binding RepoName}" />
				            <Run Text=" · " />
				            <Run Text="{Binding Type}" />
				        </TextBlock>
				        </StackPanel>
				        <TextBlock Margin="0,6,0,0" TextWrapping="Wrap" Text="{Binding Title}" Style="{StaticResource BodyTextBlockStyle}" />
				            <TextBlock Margin="0,6,0,0" TextWrapping="Wrap" Text="{Binding Description}" Style="{StaticResource CaptionTextBlockStyle}" />
				        </StackPanel>
				        <StackPanel VerticalAlignment="Top" HorizontalAlignment="Right" Grid.Column="2">
				        <TextBlock HorizontalAlignment="Right" Text="{Binding Timestamp}" Style="{StaticResource CaptionTextBlockStyle}" />
				            <StackPanel HorizontalAlignment="Right" Margin="0,8,0,0" Orientation="Horizontal">
				            <NumberBox SmallChange="5" Minimum="0" Value="{Binding DurationMinutes}" Width="72" x:Uid="MainPage.NumberBox.Duration" />
				                <Button Margin="8,0,0,0" Style="{StaticResource DefaultButtonStyle}" CommandParameter="{Binding}" Command="{Binding EditDurationCommand ElementName=ActivitiesList}" Content="Edit" />
				            </StackPanel>
				            <StackPanel HorizontalAlignment="Right" Margin="0,8,0,0" Orientation="Horizontal">
				                <CheckBox Content="Exporter" IsChecked="{Binding MarkForExport}" x:Uid="MainPage.Check.Export" />
				            </StackPanel>
				        </StackPanel>
				        <Grid.ColumnDefinitions>
				        <ColumnDefinition Width="48" />
				        <ColumnDefinition Width="*" />
				            <ColumnDefinition Width="120" />
				        </Grid.ColumnDefinitions>
				    </Grid>
				    <Border.Shadow><ThemeShadow /></Border.Shadow>
				    </Border>
				</DataTemplate></ListView.ItemTemplate>
				</ListView>
				<Border Visibility="Collapsed" Margin="12,0,0,0" Padding="12,12,12,12" Grid.Column="1" x:Name="DetailPanel">
				<StackPanel>
				<TextBlock Text="{Binding SelectedActivity.Title}" Style="{StaticResource TitleTextBlockStyle}" x:Uid="MainPage.Detail.Title" />
				    <TextBlock Margin="0,8,0,0" TextWrapping="Wrap" Text="{Binding SelectedActivity.Description}" Style="{StaticResource BodyTextBlockStyle}" />
				    <TextBlock Margin="0,12,0,0" Text="Durée" Style="{StaticResource CaptionTextBlockStyle}" />
				    <NumberBox SmallChange="5" Minimum="0" Value="{Binding SelectedActivity.DurationMinutes}" x:Uid="MainPage.Detail.Duration" />
				    <StackPanel Margin="0,16,0,0" Orientation="Horizontal">
				    <Button Style="{StaticResource AccentButtonStyle}" CommandParameter="{Binding SelectedActivity}" Command="{Binding ApproveCommand}" Content="Approuver" x:Uid="MainPage.Detail.Button.Approve" />
				        <Button Margin="8,0,0,0" Style="{StaticResource DefaultButtonStyle}" CommandParameter="{Binding SelectedActivity}" Command="{Binding SkipCommand}" Content="Ignorer" x:Uid="MainPage.Detail.Button.Skip" />
				    </StackPanel>
				</StackPanel>
				</Border>
				<Grid.ColumnDefinitions>
				<ColumnDefinition Width="2*" />
				    <ColumnDefinition x:Name="PreviewColumn" Width="0" MinWidth="280" MaxWidth="420" />
				</Grid.ColumnDefinitions>
				</Grid>
				<StackPanel Margin="0,12,0,0" HorizontalAlignment="Center" Orientation="Horizontal" Grid.Row="1">
				    <Button Style="{StaticResource DefaultButtonStyle}" Command="{Binding LoadMoreCommand}" Content="Charger plus" x:Uid="MainPage.Button.LoadMore" />
				</StackPanel>
				<Grid.RowDefinitions>
				<RowDefinition Height="56" />
				    <RowDefinition Height="*" />
				</Grid.RowDefinitions>
				</Grid>
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 39, 37, 39, 37).WithArguments("Missing comma between members of 'Binding'"),
		]);

		await test.RunAsync();
	}
}
