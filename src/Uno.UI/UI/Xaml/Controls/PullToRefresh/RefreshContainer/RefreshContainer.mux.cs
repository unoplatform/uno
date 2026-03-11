using System.Linq;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl, IRefreshContainerPrivate
{
	private const int MAX_BFS_DEPTH = 10;
	private const int DEFAULT_PULL_DIMENSION_SIZE = 100;

	// Change to 'true' to turn on debugging outputs in Output window
	//private bool PTRTraces_IsDebugOutputEnabled = false;
	//private bool PTRTraces_IsVerboseDebugOutputEnabled = false;

	// RefreshContainer is the top level visual in a PTR experience. responsible for displaying its content property along with a RefreshVisualizer,
	// in the specified location. It also adapts a member of its contents tree to an IRefreshInfoProvider and attaches it to the
	// RefreshVisualizer.
	//
	// Here is the object map of the PTR experience:
	//
	//                                          RefreshContainer
	//                                          |        |     \
	//                                          V     *Visual   \
	//                                  ___Adapter_    tree*     \
	//                                 /     |     \     |        \
	//                                /      V      \    |         \
	//                               /  Animation ___\___|_______   \
	//                              /    Handler \__  \  |       \   \
	//                             |        |       |  | |        |   |
	//                             |        V       V  V V        V   V
	//                             |   Interaction   Scroll       Refresh
	//                             |     Tracker     Viewer       Visualizer
	//                              \          \       |          /
	//                               \       *owner*   |         /
	//                                \          \     V        /
	//                                 \          \.Refresh   /
	//                                  \__________. Info <-_/
	//                                               Provider

	~RefreshContainer()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_refreshVisualizer is { } refreshVisualizer)
		{
			m_refreshVisualizerSizeChangedToken.Disposable = null;
		}
	}

	/// <summary>
	/// Initializes a new instance of the RefreshContainer control.
	/// </summary>
	public RefreshContainer()
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_RefreshContainer);

		this.SetDefaultStyleKey();

#if HAS_UNO
		SetDefaultRefreshInfoProviderAdapter();
#endif
		m_hasDefaultRefreshInfoProviderAdapter = true;
		OnRefreshInfoProviderAdapterChanged();

#if HAS_UNO
		InitializePlatformPartial();
#endif
	}

	partial void InitializePlatformPartial();

	protected override void OnApplyTemplate()
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);
		// BEGIN: Populate template children
		//IControlProtected thisAsControlProtected = this;
		m_root = (Panel)GetTemplateChild("Root");
		m_refreshVisualizerPresenter = (Panel)GetTemplateChild("RefreshVisualizerPresenter");
		// END: Populate template children

		if (m_root != null)
		{
			var rootVisual = ElementCompositionPreview.GetElementVisual(m_root);
			rootVisual.Clip = rootVisual.Compositor.CreateInsetClip(0.0f, 0.0f, 0.0f, 0.0f);
		}

		m_refreshVisualizer = Visualizer;
		if (m_refreshVisualizer == null)
		{
#if HAS_UNO
			SetDefaultRefreshVisualizer();
#endif
			m_hasDefaultRefreshVisualizer = true;
		}
		else
		{
			OnRefreshVisualizerChangedImpl();
			m_hasDefaultRefreshVisualizer = false;
		}

		m_refreshPullDirection = PullDirection;
		OnPullDirectionChangedImpl();
#if HAS_UNO
		OnApplyTemplatePartial();
#endif
	}

	partial void OnApplyTemplatePartial();

	/// <summary>
	/// Initiates an update of the content.
	/// </summary>
	public void RequestRefresh()
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);
		if (m_refreshVisualizer != null)
		{
			m_refreshVisualizer.RequestRefresh();
		}
	}

	//Privates
	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;
		if (property == VisualizerProperty)
		{
			OnRefreshVisualizerChanged(args);
		}
		else if (property == PullDirectionProperty)
		{
			OnPullDirectionChanged(args);
		}
	}

	private void OnRefreshVisualizerChanged(DependencyPropertyChangedEventArgs args)
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH_PTR_PTR, METH_NAME, this, args.OldValue(), args.NewValue());
		if (m_refreshVisualizer != null && m_refreshVisualizerSizeChangedToken.Disposable != null)
		{
			m_refreshVisualizerSizeChangedToken.Disposable = null;
		}

		m_refreshVisualizer = Visualizer;
		m_hasDefaultRefreshVisualizer = false;
		OnRefreshVisualizerChangedImpl();
	}

	private void OnRefreshVisualizerChangedImpl()
	{
		if (m_refreshVisualizerPresenter != null)
		{
			m_refreshVisualizerPresenter.Children.Clear();
			if (m_refreshVisualizer != null)
			{
				m_refreshVisualizerPresenter.Children.Add(m_refreshVisualizer);
			}
		}
		OnRefreshVisualizerChangedPartial();

		if (m_refreshVisualizer != null)
		{
			m_refreshVisualizer.SizeChanged += OnVisualizerSizeChanged;
			m_refreshVisualizer.RefreshRequested += OnVisualizerRefreshRequested;
		}
	}

	partial void OnRefreshVisualizerChangedPartial();

	private void OnPullDirectionChanged(DependencyPropertyChangedEventArgs args)
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.OldValue(), args.NewValue());
		m_refreshPullDirection = PullDirection;
		OnPullDirectionChangedImpl();
	}

	private void OnPullDirectionChangedImpl()
	{
		if (m_refreshVisualizerPresenter != null)
		{
			switch (m_refreshPullDirection)
			{
				case RefreshPullDirection.TopToBottom:
					m_refreshVisualizerPresenter.VerticalAlignment = VerticalAlignment.Top;
					m_refreshVisualizerPresenter.HorizontalAlignment = HorizontalAlignment.Stretch;
					if (m_hasDefaultRefreshVisualizer)
					{
						((IRefreshVisualizerPrivate)m_refreshVisualizer).SetInternalPullDirection(RefreshPullDirection.TopToBottom);
						m_refreshVisualizer.Height = DEFAULT_PULL_DIMENSION_SIZE;
						m_refreshVisualizer.Width = double.NaN;
					}
					break;
				case RefreshPullDirection.LeftToRight:
					m_refreshVisualizerPresenter.VerticalAlignment = VerticalAlignment.Stretch;
					m_refreshVisualizerPresenter.HorizontalAlignment = HorizontalAlignment.Left;
					if (m_hasDefaultRefreshVisualizer)
					{
						((IRefreshVisualizerPrivate)m_refreshVisualizer).SetInternalPullDirection(RefreshPullDirection.LeftToRight);
						m_refreshVisualizer.Height = double.NaN;
						m_refreshVisualizer.Width = DEFAULT_PULL_DIMENSION_SIZE;
					}
					break;
				case RefreshPullDirection.RightToLeft:
					m_refreshVisualizerPresenter.VerticalAlignment = VerticalAlignment.Stretch;
					m_refreshVisualizerPresenter.HorizontalAlignment = HorizontalAlignment.Right;
					if (m_hasDefaultRefreshVisualizer)
					{
						((IRefreshVisualizerPrivate)m_refreshVisualizer).SetInternalPullDirection(RefreshPullDirection.RightToLeft);
						m_refreshVisualizer.Height = double.NaN;
						m_refreshVisualizer.Width = DEFAULT_PULL_DIMENSION_SIZE;
					}
					break;
				case RefreshPullDirection.BottomToTop:
					m_refreshVisualizerPresenter.VerticalAlignment = VerticalAlignment.Bottom;
					m_refreshVisualizerPresenter.HorizontalAlignment = HorizontalAlignment.Stretch;
					if (m_hasDefaultRefreshVisualizer)
					{
						((IRefreshVisualizerPrivate)m_refreshVisualizer).SetInternalPullDirection(RefreshPullDirection.BottomToTop);
						m_refreshVisualizer.Height = DEFAULT_PULL_DIMENSION_SIZE;
						m_refreshVisualizer.Width = double.NaN;
					}
					break;
				default:
					MUX_ASSERT(false);
					break;
			}

			//If we have changed the PullDirection but the Adapter wont be updated by the size changed handler.
			if (m_hasDefaultRefreshInfoProviderAdapter &&
				m_hasDefaultRefreshVisualizer &&
				m_refreshVisualizer.ActualHeight == DEFAULT_PULL_DIMENSION_SIZE &&
				m_refreshVisualizer.ActualWidth == DEFAULT_PULL_DIMENSION_SIZE)
			{
#if HAS_UNO
				SetDefaultRefreshInfoProviderAdapter();
#endif
				OnRefreshInfoProviderAdapterChanged();
			}

		}
	}

	private void OnRefreshInfoProviderAdapterChanged()
	{
		if (m_root != null)
		{
			IRefreshInfoProvider firstChildAsInfoProvider = m_root.Children.FirstOrDefault() as IRefreshInfoProvider;
			if (firstChildAsInfoProvider != null)
			{
				((IRefreshVisualizerPrivate)m_refreshVisualizer).InfoProvider = firstChildAsInfoProvider;
			}
			else
			{
				IRefreshInfoProvider adaptFromTreeResult = null;
				if (m_refreshInfoProviderAdapter != null)
				{
					adaptFromTreeResult = m_refreshInfoProviderAdapter.AdaptFromTree(m_root, m_refreshVisualizer.RenderSize);
					if (adaptFromTreeResult != null)
					{
						((IRefreshVisualizerPrivate)m_refreshVisualizer).InfoProvider = adaptFromTreeResult;
#if HAS_UNO && !__ANDROID__ && !__IOS__
						(m_refreshInfoProviderAdapter as ScrollViewerIRefreshInfoProviderAdapter)?.SetupVisualizer(m_refreshVisualizer);
#endif
						m_refreshInfoProviderAdapter.SetAnimations(m_refreshVisualizer);
					}
				}
				if (adaptFromTreeResult == null)
				{
					((IRefreshVisualizerPrivate)m_refreshVisualizer).InfoProvider = SearchTreeForIRefreshInfoProvider();
				}
			}
		}
	}

	private IRefreshInfoProvider SearchTreeForIRefreshInfoProvider()
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);
		if (m_root != null)
		{
			IRefreshInfoProvider rootAsIRIP = m_root as IRefreshInfoProvider;
			int depth = 0;
			if (rootAsIRIP != null)
			{
				return rootAsIRIP;
			}
			else
			{
				while (depth < MAX_BFS_DEPTH)
				{
					IRefreshInfoProvider result = SearchTreeForIRefreshInfoProviderRecursiveHelper(m_root, depth);
					if (result != null)
					{
						return result;
					}
					depth++;
				}
			}
		}

		return null;
	}

	private IRefreshInfoProvider SearchTreeForIRefreshInfoProviderRecursiveHelper(DependencyObject root, int depth)
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH_INT, METH_NAME, this, depth);
		int numChildren = VisualTreeHelper.GetChildrenCount(root);
		if (depth == 0)
		{
			for (int i = 0; i < numChildren; i++)
			{
				DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
				IRefreshInfoProvider childObjectAsIRIP = childObject as IRefreshInfoProvider;
				if (childObjectAsIRIP != null)
				{
					return childObjectAsIRIP;
				}
			}
			return null;
		}
		else
		{
			for (int i = 0; i < numChildren; i++)
			{
				DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
				IRefreshInfoProvider recursiveResult = SearchTreeForIRefreshInfoProviderRecursiveHelper(childObject, depth - 1);
				if (recursiveResult != null)
				{
					return recursiveResult;
				}
			}
			return null;
		}
	}

	private void OnVisualizerSizeChanged(object sender, SizeChangedEventArgs args)
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH_FLT_FLT_FLT_FLT, METH_NAME, this, args.PreviousSize().Width, args.PreviousSize().Height, args.NewSize().Width, args.NewSize().Height);
		if (m_hasDefaultRefreshInfoProviderAdapter)
		{
#if HAS_UNO
			SetDefaultRefreshInfoProviderAdapter();
#endif
			OnRefreshInfoProviderAdapterChanged();
		}
	}

	private void OnVisualizerRefreshRequested(object sender, RefreshRequestedEventArgs args)
	{
		//PTR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);
		m_visualizerRefreshCompletedDeferral = args.GetDeferral();
		RaiseRefreshRequested();
	}

	private void RaiseRefreshRequested()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		var deferral = new Deferral(() =>
		{
			// CheckThread();
			RefreshCompleted();
		});

		var args = new RefreshRequestedEventArgs(deferral);

		//This makes sure that everyone registered for this event can get access to the deferral
		//Otherwise someone could complete the deferral before someone else has had a chance to grab it
		args.IncrementDeferralCount();
		RefreshRequested?.Invoke(this, args);
		args.DecrementDeferralCount();
	}

	private void RefreshCompleted()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_visualizerRefreshCompletedDeferral != null)
		{
			m_visualizerRefreshCompletedDeferral.Complete();
		}
	}

	//Private interface implementations

	IRefreshInfoProviderAdapter IRefreshContainerPrivate.RefreshInfoProviderAdapter
	{
		get
		{
			//PTR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);
			return m_refreshInfoProviderAdapter;
		}
		set
		{
			//PTR_TRACE_INFO(this, TRACE_MSG_METH_PTR, METH_NAME, this, value);
			m_refreshInfoProviderAdapter = value;
			m_hasDefaultRefreshInfoProviderAdapter = false;
			OnRefreshInfoProviderAdapterChanged();
		}
	}
}
