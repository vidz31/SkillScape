import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import ReactFlow, {
  MiniMap,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  MarkerType,
  Handle,
  Position,
  useReactFlow,
  ReactFlowProvider
} from 'reactflow';
import 'reactflow/dist/style.css';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  FlaskConical, Briefcase, GraduationCap, Code, Server, ShieldAlert, Sparkles, 
  ChevronRight, ZoomIn, ZoomOut, Maximize2, X, Plus, Minus, Info, ArrowRight,
  TrendingUp, Award, School, Landmark, ChevronDown, Search, Flame, AlertTriangle,
  Brain, DollarSign, Layers, BookOpen, Compass
} from 'lucide-react';
import { careerGuidanceApi } from '@/services/api/careerGuidance';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import { Link } from 'react-router-dom';

// Map color themes dynamically for UI accents
const COLOR_THEMES = {
  blue: { bg: 'bg-blue-50/70 dark:bg-blue-950/40', border: 'border-blue-200 dark:border-blue-500/50', text: 'text-blue-600 dark:text-blue-400', glow: 'shadow-blue-100/50 dark:shadow-blue-500/20' },
  green: { bg: 'bg-green-50/70 dark:bg-green-950/40', border: 'border-green-200 dark:border-green-500/50', text: 'text-green-600 dark:text-green-400', glow: 'shadow-green-100/50 dark:shadow-green-500/20' },
  purple: { bg: 'bg-purple-50/70 dark:bg-purple-950/40', border: 'border-purple-200 dark:border-purple-500/50', text: 'text-purple-600 dark:text-purple-400', glow: 'shadow-purple-100/50 dark:shadow-purple-500/20' },
  cyan: { bg: 'bg-cyan-50/70 dark:bg-cyan-950/40', border: 'border-cyan-200 dark:border-cyan-500/50', text: 'text-cyan-600 dark:text-cyan-400', glow: 'shadow-cyan-100/50 dark:shadow-cyan-500/20' },
  pink: { bg: 'bg-pink-50/70 dark:bg-pink-950/40', border: 'border-pink-200 dark:border-pink-500/50', text: 'text-pink-600 dark:text-pink-400', glow: 'shadow-pink-100/50 dark:shadow-pink-500/20' },
  indigo: { bg: 'bg-indigo-50/70 dark:bg-indigo-950/40', border: 'border-indigo-200 dark:border-indigo-500/50', text: 'text-indigo-600 dark:text-indigo-400', glow: 'shadow-indigo-100/50 dark:shadow-indigo-500/20' },
  rose: { bg: 'bg-rose-50/70 dark:bg-rose-950/40', border: 'border-rose-200 dark:border-rose-500/50', text: 'text-rose-600 dark:text-rose-400', glow: 'shadow-rose-100/50 dark:shadow-rose-500/20' },
  violet: { bg: 'bg-violet-50/70 dark:bg-violet-950/40', border: 'border-violet-200 dark:border-violet-500/50', text: 'text-violet-600 dark:text-violet-400', glow: 'shadow-violet-100/50 dark:shadow-violet-500/20' },
  fuchsia: { bg: 'bg-fuchsia-50/70 dark:bg-fuchsia-950/40', border: 'border-fuchsia-200 dark:border-fuchsia-500/50', text: 'text-fuchsia-600 dark:text-fuchsia-400', glow: 'shadow-fuchsia-100/50 dark:shadow-fuchsia-500/20' },
  emerald: { bg: 'bg-emerald-50/70 dark:bg-emerald-950/40', border: 'border-emerald-200 dark:border-emerald-500/50', text: 'text-emerald-600 dark:text-emerald-400', glow: 'shadow-emerald-100/50 dark:shadow-emerald-500/20' },
  amber: { bg: 'bg-amber-50/70 dark:bg-amber-950/40', border: 'border-amber-200 dark:border-amber-500/50', text: 'text-amber-600 dark:text-amber-400', glow: 'shadow-amber-100/50 dark:shadow-amber-500/20' },
  red: { bg: 'bg-red-50/70 dark:bg-red-950/40', border: 'border-red-200 dark:border-red-500/50', text: 'text-red-600 dark:text-red-400', glow: 'shadow-red-100/50 dark:shadow-red-500/20' }
};

const getIcon = (iconName) => {
  switch (iconName?.toLowerCase()) {
    case 'flask': return <FlaskConical className="w-5 h-5" />;
    case 'banknote': return <Landmark className="w-5 h-5" />;
    case 'palette': return <Plus className="w-5 h-5" />;
    case 'binary': return <FlaskConical className="w-5 h-5" />;
    case 'dna': return <FlaskConical className="w-5 h-5" />;
    case 'cpu': return <Server className="w-5 h-5" />;
    case 'heart-pulse': return <FlaskConical className="w-5 h-5" />;
    case 'code': return <Code className="w-5 h-5" />;
    case 'sparkles': return <Sparkles className="w-5 h-5" />;
    case 'globe': return <Code className="w-5 h-5" />;
    case 'server': return <Server className="w-5 h-5" />;
    case 'shield-alert': return <ShieldAlert className="w-5 h-5" />;
    default: return <Briefcase className="w-5 h-5" />;
  }
};

// Edge color dynamic calculation
const getStreamEdgeColor = (path) => {
  const color = path.color?.toLowerCase();
  const id = path.id?.toLowerCase();
  if (id?.startsWith('sci') || color === 'blue' || color === 'cyan') return '#00d2ff'; // science neon blue
  if (id?.startsWith('com') || color === 'green' || color === 'emerald') return '#00ff66'; // commerce neon green
  if (id?.startsWith('art') || color === 'purple' || color === 'pink' || color === 'violet') return '#bd00ff'; // arts neon purple
  if (id?.startsWith('dip') || color === 'amber' || color === 'yellow') return '#ff9f00'; // gov amber
  if (id?.startsWith('swi') || color === 'rose' || color === 'red') return '#ff007f'; // switch rose
  if (id?.startsWith('enh') || color === 'cyan') return '#00f6ff'; // enhancement cyan
  return '#6366f1';
};

// Custom Node Component to show Salary indicator, High Growth, and AI Risk
const CustomCareerNode = ({ data }) => {
  const theme = COLOR_THEMES[data.color] || COLOR_THEMES.blue;
  const isExpanded = data.isExpanded;
  const hasChildren = data.hasChildren;
  const raw = data.raw || {};
  const isDark = data.isDark;

  // Salary level ($ to $$$)
  const maxSalary = raw.salaryData ? Math.max(...Object.values(raw.salaryData)) : 0;
  const salaryBadge = maxSalary > 2500000 ? '$$$' : maxSalary > 1500000 ? '$$' : '$';

  // High Growth and High Automation Risk indicators
  const isHighGrowth = raw.industryGrowth >= 12.0;
  const isHighAiRisk = raw.automationRisk >= 50.0;
  const isHighlighted = data.isHighlighted;

  return (
    <div className={`p-4 rounded-xl border backdrop-blur-md transition-all duration-300 shadow-lg ${theme.bg} ${theme.border} ${theme.glow} ${isHighlighted ? 'scale-110 border-indigo-500 border-2 shadow-indigo-500/40' : 'hover:scale-105'} min-w-[220px]`}>
      {/* Input Handle */}
      {data.level > 1 && (
        <Handle
          type="target"
          position={Position.Top}
          style={{ background: isDark ? '#475569' : '#cbd5e1', width: 8, height: 8 }}
        />
      )}

      {/* Node Header */}
      <div className="flex items-start gap-3">
        <div className={`p-2 rounded-lg bg-secondary/80 border ${theme.border} ${theme.text} mt-0.5`}>
          {getIcon(data.icon)}
        </div>
        <div className="flex-1">
          <div className="flex items-center gap-1.5 justify-between">
            <span className="text-[9px] uppercase tracking-wider font-semibold opacity-60">Level {data.level}</span>
            {/* Salary Indicator Badge */}
            <span className="text-[9px] font-bold px-1.5 py-0.5 rounded bg-secondary border border-border text-emerald-600 dark:text-emerald-400">
              {salaryBadge}
            </span>
          </div>
          <h4 className="text-sm font-bold text-foreground leading-tight mt-0.5">{data.title}</h4>
        </div>
      </div>

      {/* Indicators Section (Growth, AI Risk) */}
      {(isHighGrowth || isHighAiRisk) && (
        <div className="mt-2.5 flex flex-wrap gap-1.5">
          {isHighGrowth && (
            <span className="text-[9px] font-medium bg-orange-500/10 dark:bg-orange-950/60 border border-orange-200 dark:border-orange-500/30 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded-md flex items-center gap-0.5">
              🔥 High Growth
            </span>
          )}
          {isHighAiRisk && (
            <span className="text-[9px] font-medium bg-rose-500/10 dark:bg-rose-950/60 border border-rose-200 dark:border-rose-500/30 text-rose-600 dark:text-rose-400 px-1.5 py-0.5 rounded-md flex items-center gap-0.5">
              ⚠️ AI Risk
            </span>
          )}
        </div>
      )}

      <div className="mt-3 flex items-center justify-between gap-2 border-t border-border/60 pt-2.5">
        <button
          onClick={(e) => {
            e.stopPropagation();
            data.onViewDetails(data.raw);
          }}
          className="text-[11px] font-medium text-indigo-650 dark:text-indigo-400 hover:text-indigo-500 transition-colors"
        >
          <Info className="w-3.5 h-3.5" /> Details
        </button>

        {hasChildren && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              data.onToggleExpand(data.id);
            }}
            className="p-1 rounded bg-secondary/60 hover:bg-secondary text-muted-foreground hover:text-foreground border border-border transition-colors"
          >
            {isExpanded ? <Minus className="w-3.5 h-3.5" /> : <Plus className="w-3.5 h-3.5" />}
          </button>
        )}
      </div>

      {/* Output Handle */}
      {hasChildren && isExpanded && (
        <Handle
          type="source"
          position={Position.Bottom}
          style={{ background: '#6366f1', width: 8, height: 8 }}
        />
      )}
    </div>
  );
};

// ----------------------------------------------------
// Main Career Explorer Page (Inner Component utilizing React Flow Instance)
// ----------------------------------------------------
function CareerExplorer() {
  const navigate = useNavigate();
  const [selectedStream, setSelectedStream] = useState('After12th');
  const [pathsRaw, setPathsRaw] = useState([]);
  const [expandedNodes, setExpandedNodes] = useState(new Set());
  const [selectedCareer, setSelectedCareer] = useState(null);
  const [accepting, setAccepting] = useState(false);
  const [isDark, setIsDark] = useState(document.documentElement.classList.contains('dark'));

  // Monitor theme changes on root html element
  useEffect(() => {
    const observer = new MutationObserver(() => {
      setIsDark(document.documentElement.classList.contains('dark'));
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
    return () => observer.disconnect();
  }, []);

  const handleAcceptCareer = async (pathId) => {
    if (!pathId) return;
    try {
      setAccepting(true);
      await careerGuidanceApi.acceptCareer(pathId);
      toast.success('Career accepted! Redirecting to your custom roadmap...');
      navigate('/roadmap');
    } catch (err) {
      console.error(err);
      toast.error('Failed to accept career path.');
    } finally {
      setAccepting(false);
    }
  };
  
  // Search & Filter state
  const [searchText, setSearchText] = useState('');
  const [minSalaryFilter, setMinSalaryFilter] = useState(0);
  const [minCagrFilter, setMinCagrFilter] = useState(0);
  const [hideHighRisk, setHideHighRisk] = useState(false);
  const [highlightedNodeId, setHighlightedNodeId] = useState(null);

  // Compare panel state
  const [isCompareOpen, setIsCompareOpen] = useState(false);
  const [compareId1, setCompareId1] = useState('');
  const [compareId2, setCompareId2] = useState('');

  const location = useLocation();
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);

  const reactFlowInstance = useReactFlow();

  const STREAMS = [
    { id: 'After10th', label: 'After 10th' },
    { id: 'After12th', label: 'After 12th' },
    { id: 'AfterGraduation', label: 'After Graduation' },
    { id: 'AfterPostGraduation', label: 'After Post Grad' },
    { id: 'CareerSwitch', label: 'Career Switch' },
    { id: 'Upskilling', label: 'Upskilling' },
    { id: 'Entrepreneurship', label: 'Entrepreneurship' },
    { id: 'GovernmentJobs', label: 'Govt Jobs' },
    { id: 'InternationalEducation', label: 'Global Education' }
  ];

  const nodeTypes = useMemo(() => ({ careerNode: CustomCareerNode }), []);

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const highlight = params.get('highlight');
    if (highlight) {
      setHighlightedNodeId(highlight);
    }
  }, [location.search]);

  // Fetch Tree Paths
  const fetchPaths = async (stream) => {
    try {
      const data = await careerGuidanceApi.getPathsTree(stream);
      setPathsRaw(data);
      
      // Auto-expand root level nodes by default
      const rootIds = data.map(p => p.id);
      setExpandedNodes(new Set(rootIds));
    } catch (err) {
      toast.error('Failed to load career paths tree');
    }
  };

  useEffect(() => {
    fetchPaths(selectedStream);
  }, [selectedStream]);

  // Toggle node expansion
  const handleToggleExpand = useCallback((nodeId) => {
    setExpandedNodes((prev) => {
      const next = new Set(prev);
      if (next.has(nodeId)) {
        next.delete(nodeId);
      } else {
        next.add(nodeId);
      }
      return next;
    });
  }, []);

  // View Details Drawer
  const handleViewDetails = useCallback((career) => {
    setSelectedCareer(career);
  }, []);

  // Flat mapping recursively based on expansion state
  const flattenTree = useCallback((paths, parentX = 350, parentY = 50, depth = 1, siblingIndex = 0, siblingsCount = 1) => {
    const list = [];
    const childrenCount = paths.length;

    // Distribute X coordinates dynamically
    const spacingX = Math.max(320 - depth * 35, 200);
    const totalWidth = (childrenCount - 1) * spacingX;
    const startX = parentX - totalWidth / 2;

    paths.forEach((path, idx) => {
      const isExpanded = expandedNodes.has(path.id);
      const hasChildren = path.subCareers && path.subCareers.length > 0;
      
      const currentX = childrenCount > 1 ? startX + idx * spacingX : parentX;
      const currentY = parentY + 220; // Vertical level spacing

      // Add Current Node
      list.push({
        id: path.id,
        type: 'careerNode',
        position: { x: currentX, y: parentY },
        data: {
          id: path.id,
          title: path.title,
          level: path.level,
          icon: path.icon,
          color: path.color,
          raw: path,
          isExpanded,
          hasChildren,
          onToggleExpand: handleToggleExpand,
          onViewDetails: handleViewDetails,
          isDark
        }
      });

      // Add Edge to parent if depth > 1
      if (path.parentCareerId) {
        const edgeColor = getStreamEdgeColor(path);
        list.push({
          id: `edge-${path.parentCareerId}-${path.id}`,
          source: path.parentCareerId,
          target: path.id,
          animated: true,
          style: { stroke: edgeColor, strokeWidth: 2, filter: `drop-shadow(0 0 5px ${edgeColor}77)` },
          markerEnd: {
            type: MarkerType.ArrowClosed,
            color: edgeColor,
            width: 15,
            height: 15
          }
        });
      }

      // Add children recursively if expanded
      if (hasChildren && isExpanded) {
        const childList = flattenTree(path.subCareers, currentX, currentY, depth + 1, idx, childrenCount);
        list.push(...childList);
      }
    });

    return list;
  }, [expandedNodes, handleToggleExpand, handleViewDetails, isDark]);

  // Sync React Flow nodes and edges when tree changes
  useEffect(() => {
    if (pathsRaw.length === 0) return;

    // Lay out starting roots across the screen
    const spacingRoot = 500;
    const totalRootWidth = (pathsRaw.length - 1) * spacingRoot;
    const startRootX = 400 - totalRootWidth / 2;

    const flowElements = [];
    pathsRaw.forEach((root, idx) => {
      const rootX = startRootX + idx * spacingRoot;
      const elements = flattenTree([root], rootX, 80, 1, idx, pathsRaw.length);
      flowElements.push(...elements);
    });

    const newNodes = flowElements.filter(e => e.type === 'careerNode');
    const newEdges = flowElements.filter(e => e.id.startsWith('edge-'));

    setNodes(newNodes);
    setEdges(newEdges);
  }, [pathsRaw, expandedNodes, flattenTree, setNodes, setEdges]);

  // Generate selection list for comparison
  const allCareersList = useMemo(() => {
    const list = [];
    const addToList = (path) => {
      list.push({ id: path.id, title: path.title, raw: path });
      if (path.subCareers) {
        path.subCareers.forEach(addToList);
      }
    };
    pathsRaw.forEach(addToList);
    return list.sort((a, b) => a.title.localeCompare(b.title));
  }, [pathsRaw]);

  // Apply filters and searches to nodes and edges
  const filteredNodes = useMemo(() => {
    return nodes.map(node => {
      const raw = node.data.raw || {};
      const maxSalary = raw.salaryData ? Math.max(...Object.values(raw.salaryData)) : 0;
      
      const matchesSearch = !searchText || 
        raw.title.toLowerCase().includes(searchText.toLowerCase()) ||
        raw.tags?.toLowerCase().includes(searchText.toLowerCase());

      const matchesSalary = maxSalary >= minSalaryFilter;
      const matchesGrowth = raw.industryGrowth >= minCagrFilter;
      const matchesRisk = !hideHighRisk || raw.automationRisk < 50.0;

      const isMatch = matchesSearch && matchesSalary && matchesGrowth && matchesRisk;

      return {
        ...node,
        className: isMatch ? '' : 'opacity-20 pointer-events-none transition-opacity duration-300',
        data: {
          ...node.data,
          isHighlighted: highlightedNodeId === node.id || (isMatch && searchText !== '')
        }
      };
    });
  }, [nodes, searchText, minSalaryFilter, minCagrFilter, hideHighRisk, highlightedNodeId]);

  const filteredEdges = useMemo(() => {
    return edges.map(edge => {
      const sourceNode = filteredNodes.find(n => n.id === edge.source);
      const targetNode = filteredNodes.find(n => n.id === edge.target);
      const isEdgeTranslucent = sourceNode?.className?.includes('opacity-20') || targetNode?.className?.includes('opacity-20');
      return {
        ...edge,
        style: {
          ...edge.style,
          opacity: isEdgeTranslucent ? 0.1 : 1,
          transition: 'opacity 0.3s'
        }
      };
    });
  }, [edges, filteredNodes]);

  // Mind-map search action
  const handleSearchSubmit = (e) => {
    e.preventDefault();
    if (!searchText) return;
    const matchedNode = nodes.find(n => 
      n.data.title.toLowerCase().includes(searchText.toLowerCase()) || 
      n.data.raw.tags?.toLowerCase().includes(searchText.toLowerCase())
    );
    if (matchedNode) {
      const { x, y } = matchedNode.position;
      reactFlowInstance.setCenter(x + 110, y + 50, { zoom: 1.2, duration: 800 });
      setHighlightedNodeId(matchedNode.id);
      setTimeout(() => setHighlightedNodeId(null), 3000);
      toast.success(`Found and centered on: ${matchedNode.data.title}`);
    } else {
      toast.error('No matching career path found');
    }
  };

  // Expand all pathways recursively
  const handleExpandAll = () => {
    const allIds = new Set();
    const addIdsRecursive = (path) => {
      allIds.add(path.id);
      if (path.subCareers) {
        path.subCareers.forEach(addIdsRecursive);
      }
    };
    pathsRaw.forEach(addIdsRecursive);
    setExpandedNodes(allIds);
    toast.success('Expanded all pathways');
  };

  // Collapse all pathways to level 1 roots
  const handleCollapseAll = () => {
    const rootIds = new Set(pathsRaw.map(p => p.id));
    setExpandedNodes(rootIds);
    toast.success('Collapsed mind map to primary streams');
  };

  // Hyperlink click inside drawer to switch detail focus
  const handleRelatedCareerClick = (relatedId) => {
    const found = allCareersList.find(c => c.id === relatedId);
    if (found) {
      setSelectedCareer(found.raw);
    } else {
      toast.error('Related career path is not available in the current stream map');
    }
  };

  // Comparison details
  const compCareer1 = useMemo(() => allCareersList.find(c => c.id === compareId1)?.raw, [compareId1, allCareersList]);
  const compCareer2 = useMemo(() => allCareersList.find(c => c.id === compareId2)?.raw, [compareId2, allCareersList]);

  return (
    <div className="h-[calc(100vh-80px)] w-full flex flex-col bg-background text-foreground overflow-hidden relative">
      {/* Glow Effects */}
      <div className="absolute top-[-10%] left-[20%] w-[400px] h-[400px] bg-indigo-500/5 dark:bg-indigo-500/10 rounded-full blur-[120px]" />
      <div className="absolute bottom-[-10%] right-[10%] w-[500px] h-[500px] bg-cyan-500/5 dark:bg-cyan-500/10 rounded-full blur-[140px]" />

      {/* Control Header & Filters Toolbar */}
      <div className="z-10 p-4 border-b border-border bg-background/90 backdrop-blur-md flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-indigo-600 dark:text-indigo-400" /> Career Mind Map
          </h2>
          <p className="text-xs text-muted-foreground mt-0.5">Filter by growth potential, search paths, and compare options</p>
        </div>

        {/* Toolbar Controls */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Search bar */}
          <form onSubmit={handleSearchSubmit} className="relative flex items-center bg-secondary border border-border rounded-xl px-3 py-1.5 focus-within:border-indigo-500 transition-colors w-full sm:w-60">
            <Search className="w-4 h-4 text-muted-foreground mr-2 shrink-0" />
            <input
              type="text"
              placeholder="Search careers or tags..."
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              className="bg-transparent text-xs text-foreground placeholder-muted-foreground focus:outline-none w-full"
            />
            {searchText && (
              <button type="button" onClick={() => setSearchText('')} className="text-muted-foreground hover:text-foreground ml-1">
                <X className="w-3 h-3" />
              </button>
            )}
          </form>

          {/* Level expand tools */}
          <div className="flex bg-secondary/60 p-0.5 rounded-xl border border-border">
            <Button variant="ghost" className="h-7 text-[10px] px-2 text-muted-foreground hover:text-foreground" onClick={handleExpandAll}>
              Expand All
            </Button>
            <div className="w-px bg-border self-stretch my-1" />
            <Button variant="ghost" className="h-7 text-[10px] px-2 text-muted-foreground hover:text-foreground" onClick={handleCollapseAll}>
              Collapse All
            </Button>
          </div>

          {/* Compare trigger button */}
          <Button
            onClick={() => setIsCompareOpen(true)}
            className="h-8 text-xs bg-secondary hover:bg-secondary/80 border border-border text-indigo-600 dark:text-indigo-400 hover:text-indigo-500 flex items-center gap-1"
          >
            <Layers className="w-3.5 h-3.5" /> Compare Pathways
          </Button>

          {/* Stream Selector */}
          <div className="flex gap-1.5 bg-secondary p-1 rounded-xl border border-border">
            {STREAMS.map((s) => (
              <button
                key={s.id}
                onClick={() => setSelectedStream(s.id)}
                className={`px-3 py-1 rounded-lg text-[11px] font-semibold transition-all duration-300 ${
                  selectedStream === s.id
                    ? 'bg-gradient-to-r from-indigo-600 to-cyan-600 text-white shadow-md shadow-indigo-600/25'
                    : 'text-muted-foreground hover:text-foreground hover:bg-background/40'
                }`}
              >
                {s.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Advanced Filters Drawer (Embedded directly below header) */}
      <div className="z-10 px-6 py-3 bg-background/60 border-b border-border/80 flex flex-wrap items-center gap-6 text-xs text-muted-foreground">
        {/* Salary Slider */}
        <div className="flex items-center gap-2">
          <span className="font-semibold text-foreground">Min. Senior Salary:</span>
          <input
            type="range"
            min="1000000"
            max="3500000"
            step="100000"
            value={minSalaryFilter || 1000000}
            onChange={(e) => setMinSalaryFilter(parseInt(e.target.value))}
            className="w-24 accent-indigo-500 cursor-pointer"
          />
          <span className="text-[10px] text-indigo-600 dark:text-indigo-400 font-mono">
            ₹{(minSalaryFilter / 100000).toFixed(1)}L/yr
          </span>
          {minSalaryFilter > 0 && (
            <button onClick={() => setMinSalaryFilter(0)} className="text-rose-500 dark:text-rose-400 hover:underline text-[9px] ml-1">
              Reset
            </button>
          )}
        </div>

        {/* CAGR Growth Slider */}
        <div className="flex items-center gap-2">
          <span className="font-semibold text-foreground">Min. Growth Rate (CAGR):</span>
          <input
            type="range"
            min="0"
            max="20"
            step="1"
            value={minCagrFilter}
            onChange={(e) => setMinCagrFilter(parseInt(e.target.value))}
            className="w-24 accent-orange-500 cursor-pointer"
          />
          <span className="text-[10px] text-orange-650 dark:text-orange-400 font-mono">
            {minCagrFilter}%+
          </span>
          {minCagrFilter > 0 && (
            <button onClick={() => setMinCagrFilter(0)} className="text-rose-500 dark:text-rose-400 hover:underline text-[9px] ml-1">
              Reset
            </button>
          )}
        </div>

        {/* AI Displacement Toggle */}
        <label className="flex items-center gap-2 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={hideHighRisk}
            onChange={(e) => setHideHighRisk(e.target.checked)}
            className="rounded border-border bg-secondary text-indigo-600 focus:ring-0 w-3.5 h-3.5 cursor-pointer accent-indigo-500"
          />
          <span className="text-foreground font-semibold">Hide High AI Risk (&gt;50%)</span>
        </label>
      </div>

      {/* React Flow Workspace */}
      <div className="flex-1 w-full bg-background relative">
        <ReactFlow
          nodes={filteredNodes}
          edges={filteredEdges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          nodeTypes={nodeTypes}
          fitView
          fitViewOptions={{ padding: 0.15 }}
          minZoom={0.15}
          maxZoom={1.5}
        >
          <Background color={isDark ? "#334155" : "#cbd5e1"} gap={28} size={1.2} />
          <Controls className="bg-card border border-border rounded-lg p-1 text-foreground" />
          <MiniMap
            nodeStrokeColor={() => '#6366f1'}
            nodeColor={() => (isDark ? '#090d16' : '#ffffff')}
            maskColor={isDark ? "rgba(15, 23, 42, 0.7)" : "rgba(255, 255, 255, 0.7)"}
            className="bg-card border border-border rounded-lg"
          />
        </ReactFlow>

        {/* Floating Instruction Banner */}
        <div className="absolute bottom-6 left-6 z-10 bg-card/85 border border-border backdrop-blur-md px-3 py-2.5 rounded-lg text-[11px] text-muted-foreground flex items-center gap-2.5 max-w-sm shadow-xl pointer-events-none">
          <Info className="w-4 h-4 text-indigo-600 dark:text-indigo-400 shrink-0" />
          <span>Use mouse wheel to zoom. Drag canvas to pan. Use Details for advanced specs. Filter paths above.</span>
        </div>
      </div>

      {/* Detail Slide-over Drawer */}
      <AnimatePresence>
        {selectedCareer && (
          <>
            {/* Backdrop */}
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 0.5 }}
              exit={{ opacity: 0 }}
              onClick={() => setSelectedCareer(null)}
              className="absolute inset-0 bg-black z-20 cursor-pointer"
            />
            {/* Drawer */}
            <motion.div
              initial={{ x: '100%' }}
              animate={{ x: 0 }}
              exit={{ x: '100%' }}
              transition={{ type: 'spring', damping: 22, stiffness: 110 }}
              className="absolute top-0 right-0 h-full w-full sm:w-[480px] bg-card border-l border-border z-30 flex flex-col shadow-2xl overflow-y-auto"
            >
              {/* Header */}
              <div className="p-6 border-b border-border flex items-center justify-between sticky top-0 bg-card z-10">
                <div className="flex items-center gap-3">
                  <div className={`p-2.5 rounded-xl border bg-background text-${selectedCareer.color}-600 dark:text-${selectedCareer.color}-400 border-${selectedCareer.color}-200 dark:border-${selectedCareer.color}-500/50`}>
                    {getIcon(selectedCareer.icon)}
                  </div>
                  <div>
                    <h3 className="text-base font-bold text-foreground leading-tight">{selectedCareer.title}</h3>
                    <div className="flex items-center gap-1.5 mt-1">
                      <span className="text-[9px] font-semibold bg-secondary text-muted-foreground px-2 py-0.5 rounded uppercase tracking-wider">
                        Level {selectedCareer.level}
                      </span>
                      <span className="text-[9px] font-semibold bg-background text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 px-2 py-0.5 rounded">
                        Difficulty: {selectedCareer.difficultyLevel || 'Medium'}
                      </span>
                    </div>
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="rounded-full text-muted-foreground hover:text-foreground"
                  onClick={() => setSelectedCareer(null)}
                >
                  <X className="w-5 h-5" />
                </Button>
              </div>

              {/* Body */}
              <div className="flex-1 p-6 space-y-6">
                {/* Description */}
                <div>
                  <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2">Description</h4>
                  <p className="text-xs text-foreground leading-relaxed bg-background border border-border p-4 rounded-xl">
                    {selectedCareer.description}
                  </p>
                </div>

                {/* Core Study Details */}
                {selectedCareer.whatYouWillStudy && (
                  <div>
                    <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2 flex items-center gap-1">
                      <BookOpen className="w-3.5 h-3.5 text-indigo-650 dark:text-indigo-400" /> Syllabus Overview (What you will study)
                    </h4>
                    <p className="text-xs text-foreground leading-relaxed bg-background/30 border border-border/60 p-3.5 rounded-xl">
                      {selectedCareer.whatYouWillStudy}
                    </p>
                  </div>
                )}

                {/* Grid of details: Environment, MBTI, Degrees */}
                <div className="grid grid-cols-2 gap-4">
                  <div className="bg-background border border-border p-3.5 rounded-xl">
                    <span className="text-[10px] font-medium text-muted-foreground block mb-1">Work Environment</span>
                    <span className="text-xs font-semibold text-foreground">{selectedCareer.workEnvironment || 'Office'}</span>
                  </div>

                  <div className="bg-background border border-border p-3.5 rounded-xl">
                    <span className="text-[10px] font-medium text-muted-foreground block mb-1">MBTI Personality Match</span>
                    <span className="text-xs font-semibold text-indigo-600 dark:text-indigo-400 font-mono">{selectedCareer.personalityMatch || 'Not Specified'}</span>
                  </div>

                  <div className="bg-background border border-border p-3.5 rounded-xl">
                    <span className="text-[10px] font-medium text-muted-foreground block mb-1">Remote Work Options</span>
                    <span className="text-xs font-semibold text-foreground">{selectedCareer.remoteWorkPossibility}%</span>
                  </div>

                  <div className="bg-background border border-border p-3.5 rounded-xl">
                    <span className="text-[10px] font-medium text-muted-foreground block mb-1">Entrepreneurship Index</span>
                    <span className="text-xs font-semibold text-foreground">{selectedCareer.entrepreneurshipPotential}%</span>
                  </div>
                </div>

                {/* Recommended Subjects */}
                {selectedCareer.recommendedSubjects && selectedCareer.recommendedSubjects.length > 0 && (
                  <div>
                    <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2">Recommended Subjects</h4>
                    <div className="flex flex-wrap gap-1.5">
                      {selectedCareer.recommendedSubjects.map((sub, idx) => (
                        <span key={idx} className="text-xs bg-background text-foreground border border-border px-2.5 py-1 rounded-lg">
                          {sub}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                {/* Skills Required */}
                <div>
                  <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2">Required Skills</h4>
                  <div className="flex flex-wrap gap-1.5">
                    {selectedCareer.skillsRequired?.map((skill, index) => (
                      <span
                        key={index}
                        className="text-xs bg-background text-indigo-650 dark:text-indigo-400 border border-indigo-500/20 px-2.5 py-1 rounded-full font-medium"
                      >
                        {skill}
                      </span>
                    ))}
                  </div>
                </div>

                {/* Indian Colleges */}
                {selectedCareer.colleges && selectedCareer.colleges.length > 0 && selectedCareer.colleges[0] !== "" && (
                  <div>
                    <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2 flex items-center gap-1.5">
                      <School className="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400" /> Key Indian Colleges / Institutes
                    </h4>
                    <div className="flex flex-wrap gap-1.5">
                      {selectedCareer.colleges.map((college, index) => (
                        <span key={index} className="text-xs bg-background text-emerald-650 dark:text-emerald-400 border border-emerald-500/20 px-3 py-1 rounded-lg">
                          {college}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                {/* Learning & Youtube Resources */}
                {((selectedCareer.learningResources && selectedCareer.learningResources.length > 0 && selectedCareer.learningResources[0] !== "") ||
                  (selectedCareer.youtubeResources && selectedCareer.youtubeResources.length > 0 && selectedCareer.youtubeResources[0] !== "")) && (
                  <div>
                    <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2">Learning & Reference Resources</h4>
                    <div className="space-y-2">
                      {selectedCareer.learningResources?.map((res, index) => (
                        <div key={index} className="text-xs text-foreground bg-background/40 p-2.5 rounded-lg border border-border flex items-start gap-2">
                          <Compass className="w-4 h-4 text-indigo-650 dark:text-indigo-400 shrink-0 mt-0.5" />
                          <span>{res}</span>
                        </div>
                      ))}
                      {selectedCareer.youtubeResources?.map((yt, index) => (
                        <div key={index} className="text-xs text-foreground bg-red-500/5 p-2.5 rounded-lg border border-red-500/10 flex items-start gap-2">
                          <Flame className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />
                          <span>YouTube: {yt}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Salary, CAGR, and AI Risk stats */}
                <div className="grid grid-cols-3 gap-3">
                  <div className="bg-background border border-border p-3 rounded-xl text-center">
                    <span className="text-[9px] font-semibold text-muted-foreground block mb-1">Growth (CAGR)</span>
                    <span className="text-sm font-extrabold text-orange-650 dark:text-orange-400">{selectedCareer.industryGrowth}%</span>
                  </div>

                  <div className="bg-background border border-border p-3 rounded-xl text-center">
                    <span className="text-[9px] font-semibold text-muted-foreground block mb-1">Demand Index</span>
                    <span className="text-sm font-extrabold text-cyan-650 dark:text-cyan-400">{selectedCareer.demandIndex}%</span>
                  </div>

                  <div className="bg-background border border-border p-3 rounded-xl text-center">
                    <span className="text-[9px] font-semibold text-muted-foreground block mb-1">AI Risk</span>
                    <span className={`text-sm font-extrabold ${selectedCareer.automationRisk >= 50 ? 'text-rose-650 dark:text-rose-400' : 'text-emerald-655 dark:text-emerald-400'}`}>
                      {selectedCareer.automationRisk}%
                    </span>
                  </div>
                </div>

                {/* Related Careers Navigation */}
                {selectedCareer.relatedCareerIds && selectedCareer.relatedCareerIds.split(',').filter(Boolean).length > 0 && (
                  <div>
                    <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2">Related Careers (Network Graph)</h4>
                    <div className="flex flex-wrap gap-2">
                      {selectedCareer.relatedCareerIds.split(',').filter(Boolean).map((relId) => {
                        const careerObj = allCareersList.find(c => c.id === relId);
                        return (
                          <button
                            key={relId}
                            onClick={() => handleRelatedCareerClick(relId)}
                            className="text-xs bg-secondary hover:bg-secondary/80 border border-border text-indigo-600 dark:text-indigo-400 px-3 py-1.5 rounded-lg flex items-center gap-1.5 transition-colors"
                          >
                            <span>{careerObj?.title || relId}</span>
                            <ArrowRight className="w-3 h-3" />
                          </button>
                        );
                      })}
                    </div>
                  </div>
                )}

                {/* Scope Outlook */}
                <div>
                  <h4 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground mb-2">Scope Outlook</h4>
                  <p className="text-xs text-muted-foreground leading-relaxed bg-background/25 border border-border/60 p-3 rounded-lg">
                    {selectedCareer.futureScope}
                  </p>
                </div>
              </div>

              {/* Action Footer */}
              <div className="p-6 border-t border-border bg-card/90 sticky bottom-0 z-10 flex gap-2 flex-col sm:flex-row">
                <Button
                  onClick={() => handleAcceptCareer(selectedCareer.id)}
                  disabled={accepting}
                  className="bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-xs font-bold text-white flex-1 border-0 animate-none"
                >
                  {accepting ? (
                    <>
                      <Loader2 className="w-3.5 h-3.5 animate-spin mr-1.5" />
                      Accepting...
                    </>
                  ) : (
                    'Accept Career & Start Roadmap'
                  )}
                </Button>
                <Button
                  onClick={() => {
                    setCompareId1(selectedCareer.id);
                    setIsCompareOpen(true);
                    setSelectedCareer(null);
                    toast.success(`Set ${selectedCareer.title} as Career 1 for comparison`);
                  }}
                  className="bg-indigo-600 hover:bg-indigo-500 text-xs font-semibold text-white flex-1 border-0"
                >
                  <Layers className="w-4 h-4 mr-1.5" /> Add to Compare
                </Button>
                <Button asChild variant="outline" className="border-border bg-transparent text-muted-foreground hover:bg-secondary hover:text-foreground flex-1 text-xs">
                  <Link to={`/salary-forecast?id=${selectedCareer.id}`}>
                    Forecast Salary
                  </Link>
                </Button>
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>

      {/* Compare Pathways Sidebar Drawer */}
      <AnimatePresence>
        {isCompareOpen && (
          <>
            {/* Backdrop */}
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 0.5 }}
              exit={{ opacity: 0 }}
              onClick={() => setIsCompareOpen(false)}
              className="absolute inset-0 bg-black z-20 cursor-pointer"
            />
            {/* Drawer */}
            <motion.div
              initial={{ x: '-100%' }}
              animate={{ x: 0 }}
              exit={{ x: '-100%' }}
              transition={{ type: 'spring', damping: 22, stiffness: 110 }}
              className="absolute top-0 left-0 h-full w-full sm:w-[500px] bg-card border-r border-border z-30 flex flex-col shadow-2xl overflow-y-auto"
            >
              {/* Header */}
              <div className="p-6 border-b border-border flex items-center justify-between sticky top-0 bg-card z-10">
                <div className="flex items-center gap-2.5">
                  <Layers className="w-5 h-5 text-indigo-650 dark:text-indigo-400" />
                  <h3 className="text-base font-bold text-foreground">Compare Career Pathways</h3>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="rounded-full text-muted-foreground hover:text-foreground"
                  onClick={() => setIsCompareOpen(false)}
                >
                  <X className="w-5 h-5" />
                </Button>
              </div>

              {/* Body */}
              <div className="p-6 flex-1 space-y-6">
                {/* Selectors */}
                <div className="grid grid-cols-2 gap-4 bg-background/40 p-4 rounded-xl border border-border">
                  <div>
                    <label className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground block mb-1.5">Career 1</label>
                    <select
                      value={compareId1}
                      onChange={(e) => setCompareId1(e.target.value)}
                      className="bg-background border border-border rounded-lg p-2 text-xs text-foreground w-full focus:outline-none focus:border-indigo-500"
                    >
                      <option value="">-- Choose Career --</option>
                      {allCareersList.map(c => (
                        <option key={c.id} value={c.id} disabled={c.id === compareId2}>{c.title}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground block mb-1.5">Career 2</label>
                    <select
                      value={compareId2}
                      onChange={(e) => setCompareId2(e.target.value)}
                      className="bg-background border border-border rounded-lg p-2 text-xs text-foreground w-full focus:outline-none focus:border-indigo-500"
                    >
                      <option value="">-- Choose Career --</option>
                      {allCareersList.map(c => (
                        <option key={c.id} value={c.id} disabled={c.id === compareId1}>{c.title}</option>
                      ))}
                    </select>
                  </div>
                </div>

                {compCareer1 && compCareer2 ? (
                  <div className="space-y-4">
                    {/* Comparative Table */}
                    <div className="bg-background border border-border rounded-xl overflow-hidden text-xs">
                      {/* Metric: Title */}
                      <div className="grid grid-cols-3 border-b border-border font-bold bg-secondary/50">
                        <div className="p-3 text-muted-foreground">Metric</div>
                        <div className="p-3 text-indigo-600 dark:text-indigo-400">{compCareer1.title}</div>
                        <div className="p-3 text-cyan-600 dark:text-cyan-400">{compCareer2.title}</div>
                      </div>

                      {/* Metric: Level */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">Level</div>
                        <div className="p-3">Level {compCareer1.level}</div>
                        <div className="p-3">Level {compCareer2.level}</div>
                      </div>

                      {/* Metric: CAGR Growth */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">CAGR Growth</div>
                        <div className="p-3 text-orange-600 dark:text-orange-400 font-semibold">{compCareer1.industryGrowth}%</div>
                        <div className="p-3 text-orange-600 dark:text-orange-400 font-semibold">{compCareer2.industryGrowth}%</div>
                      </div>

                      {/* Metric: Automation Risk */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">AI displacement Risk</div>
                        <div className={`p-3 font-semibold ${compCareer1.automationRisk >= 50 ? 'text-rose-650 dark:text-rose-400' : 'text-emerald-650 dark:text-emerald-400'}`}>
                          {compCareer1.automationRisk}%
                        </div>
                        <div className={`p-3 font-semibold ${compCareer2.automationRisk >= 50 ? 'text-rose-655 dark:text-rose-400' : 'text-emerald-650 dark:text-emerald-400'}`}>
                          {compCareer2.automationRisk}%
                        </div>
                      </div>

                      {/* Metric: Work Environment */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">Work Environment</div>
                        <div className="p-3">{compCareer1.workEnvironment}</div>
                        <div className="p-3">{compCareer2.workEnvironment}</div>
                      </div>

                      {/* Metric: Remote Work Possibility */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">Remote Options</div>
                        <div className="p-3">{compCareer1.remoteWorkPossibility}%</div>
                        <div className="p-3">{compCareer2.remoteWorkPossibility}%</div>
                      </div>

                      {/* Metric: Difficulty */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">Difficulty</div>
                        <div className="p-3">{compCareer1.difficultyLevel}</div>
                        <div className="p-3">{compCareer2.difficultyLevel}</div>
                      </div>

                      {/* Metric: Personality Match */}
                      <div className="grid grid-cols-3 border-b border-border">
                        <div className="p-3 text-muted-foreground font-medium">Personality Type</div>
                        <div className="p-3 font-mono text-indigo-600 dark:text-indigo-400">{compCareer1.personalityMatch || 'N/A'}</div>
                        <div className="p-3 font-mono text-cyan-600 dark:text-cyan-400">{compCareer2.personalityMatch || 'N/A'}</div>
                      </div>
                    </div>

                    {/* Compare Skills */}
                    <div className="grid grid-cols-2 gap-4 text-xs">
                      <div className="bg-background/40 p-3 rounded-lg border border-border">
                        <span className="font-semibold text-indigo-650 dark:text-indigo-400 block mb-1">{compCareer1.title} Skills:</span>
                        <div className="flex flex-wrap gap-1 mt-1">
                          {compCareer1.skillsRequired?.map((s, i) => (
                            <span key={i} className="text-[10px] bg-secondary px-2 py-0.5 rounded text-foreground">{s}</span>
                          ))}
                        </div>
                      </div>
                      <div className="bg-background/40 p-3 rounded-lg border border-border">
                        <span className="font-semibold text-cyan-650 dark:text-cyan-400 block mb-1">{compCareer2.title} Skills:</span>
                        <div className="flex flex-wrap gap-1 mt-1">
                          {compCareer2.skillsRequired?.map((s, i) => (
                            <span key={i} className="text-[10px] bg-secondary px-2 py-0.5 rounded text-foreground">{s}</span>
                          ))}
                        </div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col items-center justify-center p-12 text-muted-foreground border border-dashed border-border rounded-xl bg-background/20">
                    <Layers className="w-8 h-8 text-muted-foreground/60 mb-2" />
                    <p className="text-xs text-center">Select two career paths above to perform a side-by-side comparative analysis of salaries, CAGR, AI risk, and required subjects.</p>
                  </div>
                )}
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </div>
  );
}

export default function CareerExplorerWrapper() {
  return (
    <ReactFlowProvider>
      <CareerExplorer />
    </ReactFlowProvider>
  );
}
