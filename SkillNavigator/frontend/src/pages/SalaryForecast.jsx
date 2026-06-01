import React, { useState, useEffect } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { 
  Sparkles, TrendingUp, Landmark, MapPin, Award, ArrowLeft, ArrowRight,
  Info, Loader2, DollarSign, Wallet, BrainCircuit, ShieldCheck
} from 'lucide-react';
import { careerGuidanceApi } from '@/services/api/careerGuidance';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend
} from 'recharts';

export default function SalaryForecast() {
  const [searchParams] = useSearchParams();
  const initialPathId = searchParams.get('id');

  const [careerPaths, setCareerPaths] = useState([]);
  const [selectedPathId, setSelectedPathId] = useState(initialPathId || '');
  const [forecast, setForecast] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isPathsLoading, setIsPathsLoading] = useState(true);

  // Theme support
  const [isDark, setIsDark] = useState(document.documentElement.classList.contains('dark'));

  // Monitor theme changes on root html element
  useEffect(() => {
    const observer = new MutationObserver(() => {
      setIsDark(document.documentElement.classList.contains('dark'));
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
    return () => observer.disconnect();
  }, []);

  // Sliders states
  const [targetYear, setTargetYear] = useState(2032);
  const [graduationYear, setGraduationYear] = useState(2028);
  const [location, setLocation] = useState('Tier 1');
  const [skillLevel, setSkillLevel] = useState('Intermediate');

  // Load all paths for selection dropdown
  useEffect(() => {
    const fetchAllPaths = async () => {
      try {
        const p1 = await careerGuidanceApi.getPathsTree('After10th');
        const p2 = await careerGuidanceApi.getPathsTree('After12th');
        const p3 = await careerGuidanceApi.getPathsTree('AfterGraduation');
        const p4 = await careerGuidanceApi.getPathsTree('AfterPostGraduation');
        const p5 = await careerGuidanceApi.getPathsTree('CareerSwitch');
        const p6 = await careerGuidanceApi.getPathsTree('Upskilling');
        const p7 = await careerGuidanceApi.getPathsTree('Entrepreneurship');
        const p8 = await careerGuidanceApi.getPathsTree('GovernmentJobs');
        const p9 = await careerGuidanceApi.getPathsTree('InternationalEducation');
        
        // Flatten paths list
        const list = [];
        const seenIds = new Set();
        const flatten = (arr) => {
          if (!arr) return;
          arr.forEach(p => {
            if (p.level >= 4) { // Only show specialization roles (level 4/5)
              if (!seenIds.has(p.id)) {
                seenIds.add(p.id);
                list.push({ id: p.id, title: p.title });
              }
            }
            if (p.subCareers) flatten(p.subCareers);
          });
        };
        
        flatten(p1);
        flatten(p2);
        flatten(p3);
        flatten(p4);
        flatten(p5);
        flatten(p6);
        flatten(p7);
        flatten(p8);
        flatten(p9);
        
        setCareerPaths(list);
        if (!selectedPathId && list.length > 0) {
          setSelectedPathId(list[0].id);
        }
      } catch (err) {
        toast.error('Failed to load career paths list');
      } finally {
        setIsPathsLoading(false);
      }
    };
    fetchAllPaths();
  }, []);

  // Fetch forecast data on slider/input changes
  const runForecast = async () => {
    if (!selectedPathId) return;
    setIsLoading(true);
    try {
      const data = await careerGuidanceApi.forecastSalary({
        careerPathId: selectedPathId,
        targetYear,
        expectedGraduationYear: graduationYear,
        location,
        skillLevel
      });
      setForecast(data);
    } catch (err) {
      toast.error('Failed to calculate salary forecast');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    runForecast();
  }, [selectedPathId, targetYear, graduationYear, location, skillLevel]);

  // Format currency numbers beautifully (Indian Lakhs)
  const formatINR = (val) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(val);
  };

  // Active theme properties
  const gridColor = isDark ? '#1e293b' : '#e2e8f0';
  const labelColor = isDark ? '#94a3b8' : '#475569';
  const tooltipStyle = isDark 
    ? { backgroundColor: '#0f172a', borderColor: '#1e293b', borderRadius: '12px', color: '#f8fafc' }
    : { backgroundColor: '#ffffff', borderColor: '#e2e8f0', borderRadius: '12px', color: '#0f172a' };
  const tooltipLabelStyle = { color: isDark ? '#ffffff' : '#0f172a', fontSize: '11px', fontWeight: 'bold' };

  return (
    <div className="min-h-[calc(100vh-80px)] bg-background text-foreground p-4 md:p-8 relative overflow-y-auto transition-colors duration-300">
      {/* Background glowing overlays */}
      <div className="absolute top-[-5%] left-[10%] w-[500px] h-[500px] bg-indigo-500/5 dark:bg-indigo-500/10 rounded-full blur-[130px] pointer-events-none" />
      <div className="absolute bottom-[-10%] right-[10%] w-[400px] h-[400px] bg-cyan-500/5 dark:bg-cyan-500/10 rounded-full blur-[110px] pointer-events-none" />

      <div className="max-w-5xl mx-auto space-y-6 relative z-10">
        
        {/* Header Navigation */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <Button asChild variant="ghost" className="text-muted-foreground hover:text-foreground pl-0 text-xs gap-1.5 mb-2 sm:mb-0">
              <Link to="/career-explorer">
                <ArrowLeft className="w-4 h-4" /> Back to Mind Map
              </Link>
            </Button>
            <h1 className="text-2xl md:text-3xl font-black text-foreground flex items-center gap-2">
              <TrendingUp className="w-6 h-6 text-indigo-600 dark:text-indigo-400" /> Salary Forecast Engine
            </h1>
            <p className="text-xs text-muted-foreground mt-0.5">Model expected salary cagr trends based on skills, geography, and graduation timelines</p>
          </div>
        </div>

        {/* Dashboard layout Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          
          {/* Controls Panel (1 Column) */}
          <div className="bg-card border border-border backdrop-blur-md rounded-2xl p-6 space-y-6">
            <h3 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border/60 pb-3">
              <BrainCircuit className="w-4.5 h-4.5 text-indigo-600 dark:text-indigo-400" /> Forecast Parameters
            </h3>

            {/* Career Selector */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Select Career Role</label>
              {isPathsLoading ? (
                <div className="h-10 bg-background border border-border rounded-xl flex items-center justify-center">
                  <Loader2 className="w-5 h-5 text-indigo-500 animate-spin" />
                </div>
              ) : (
                <select
                  value={selectedPathId}
                  onChange={(e) => setSelectedPathId(e.target.value)}
                  className="w-full bg-background border border-border rounded-xl p-3 text-sm font-semibold text-foreground focus:outline-none focus:border-indigo-500/50"
                >
                  {careerPaths.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.title}
                    </option>
                  ))}
                </select>
              )}
            </div>

            {/* Target Year Slider */}
            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Target Year</label>
                <span className="text-xs font-bold text-indigo-600 dark:text-indigo-400">{targetYear}</span>
              </div>
              <input
                type="range"
                min="2026"
                max="2036"
                value={targetYear}
                onChange={(e) => setTargetYear(parseInt(e.target.value))}
                className="w-full h-1.5 bg-background rounded-lg appearance-none cursor-pointer accent-indigo-500"
              />
              <div className="flex justify-between text-[9px] text-muted-foreground">
                <span>2026</span>
                <span>2031</span>
                <span>2036</span>
              </div>
            </div>

            {/* Expected Graduation Year Slider */}
            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Expected Graduation</label>
                <span className="text-xs font-bold text-cyan-600 dark:text-cyan-400">{graduationYear}</span>
              </div>
              <input
                type="range"
                min="2026"
                max="2034"
                value={graduationYear}
                onChange={(e) => setGraduationYear(parseInt(e.target.value))}
                className="w-full h-1.5 bg-background rounded-lg appearance-none cursor-pointer accent-cyan-500"
              />
              <div className="flex justify-between text-[9px] text-muted-foreground">
                <span>2026</span>
                <span>2030</span>
                <span>2034</span>
              </div>
            </div>

            {/* Geographic Location Tier */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-1">
                <MapPin className="w-3.5 h-3.5 text-rose-500 dark:text-rose-400" /> Geographic Zone
              </label>
              <div className="grid grid-cols-2 gap-2">
                {['Tier 1', 'Tier 2', 'Tier 3', 'Global'].map((loc) => (
                  <button
                    key={loc}
                    onClick={() => setLocation(loc)}
                    className={`py-2 px-3 text-xs rounded-xl font-bold border transition-all duration-300 ${
                      location === loc
                        ? 'bg-indigo-500/10 dark:bg-indigo-950/40 border-indigo-500 text-indigo-600 dark:text-indigo-400'
                        : 'bg-background border-border text-muted-foreground hover:text-foreground'
                    }`}
                  >
                    {loc}
                  </button>
                ))}
              </div>
            </div>

            {/* Target Skill Level */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-1">
                <Award className="w-3.5 h-3.5 text-amber-500 dark:text-amber-400" /> Target Skill Tier
              </label>
              <div className="grid grid-cols-3 gap-2">
                {['Beginner', 'Intermediate', 'Expert'].map((lvl) => (
                  <button
                    key={lvl}
                    onClick={() => setSkillLevel(lvl)}
                    className={`py-2 px-1 text-[11px] rounded-xl font-bold border transition-all duration-300 ${
                      skillLevel === lvl
                        ? 'bg-cyan-500/10 dark:bg-cyan-950/40 border-cyan-500 text-cyan-600 dark:text-cyan-400'
                        : 'bg-background border-border text-muted-foreground hover:text-foreground'
                    }`}
                  >
                    {lvl}
                  </button>
                ))}
              </div>
            </div>

          </div>

          {/* Predictions & Charts Display (2 Columns) */}
          <div className="lg:col-span-2 space-y-6">
            
            {/* Overlay Loader */}
            {isLoading && !forecast && (
              <div className="h-[400px] bg-card border border-border rounded-2xl flex items-center justify-center">
                <Loader2 className="w-10 h-10 text-indigo-500 animate-spin" />
              </div>
            )}

            {forecast && (
              <>
                {/* Neon Summary Cards Grid */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  
                  {/* Predicted Salary */}
                  <div className="bg-gradient-to-b from-indigo-500/5 to-card border border-indigo-500/20 dark:from-indigo-950/30 dark:to-slate-900/30 border-border/50 p-5 rounded-2xl shadow-lg">
                    <span className="text-[10px] uppercase font-bold text-indigo-600 dark:text-indigo-400">Predicted Salary ({forecast.targetYear})</span>
                    <p className="text-2xl font-black text-foreground mt-2 leading-tight">
                      {formatINR(forecast.predictedSalary)}
                    </p>
                    <p className="text-[10px] text-muted-foreground mt-1">Expected starting base salary</p>
                  </div>

                  {/* Salary Range */}
                  <div className="bg-gradient-to-b from-cyan-500/5 to-card border border-cyan-500/20 dark:from-cyan-950/30 dark:to-slate-900/30 border-border/50 p-5 rounded-2xl shadow-lg">
                    <span className="text-[10px] uppercase font-bold text-cyan-600 dark:text-cyan-400">Confidence Range</span>
                    <p className="text-lg font-black text-foreground mt-2.5 leading-tight">
                      {formatINR(forecast.predictedSalaryMin)} - {formatINR(forecast.predictedSalaryMax)}
                    </p>
                    <p className="text-[10px] text-muted-foreground mt-1.5">Based on volatility trends</p>
                  </div>

                  {/* India vs Global Avg */}
                  <div className="bg-gradient-to-b from-emerald-500/5 to-card border border-emerald-500/20 dark:from-emerald-950/30 dark:to-slate-900/30 border-border/50 p-5 rounded-2xl shadow-lg">
                    <span className="text-[10px] uppercase font-bold text-emerald-600 dark:text-emerald-400">Geographic Index</span>
                    <div className="mt-2 text-xs space-y-1">
                      <div className="flex justify-between text-muted-foreground">
                        <span>India Avg:</span>
                        <span className="font-bold text-foreground">{formatINR(forecast.indiaSalaryAvg)}</span>
                      </div>
                      <div className="flex justify-between text-muted-foreground">
                        <span>Global Avg:</span>
                        <span className="font-bold text-foreground">{formatINR(forecast.globalSalaryAvg)}</span>
                      </div>
                    </div>
                  </div>

                </div>

                {/* Trajectory Recharts Chart */}
                <div className="bg-card border border-border/80 rounded-2xl p-6 backdrop-blur-md">
                  <div className="flex justify-between items-center mb-6">
                    <div>
                      <h4 className="font-bold text-sm text-foreground">Compound Salary Trajectory</h4>
                      <p className="text-[10px] text-muted-foreground mt-0.5">Estimated CAGR comparison over the years</p>
                    </div>
                    <span className="text-[10px] font-semibold bg-background text-indigo-600 dark:text-indigo-400 border border-indigo-500/20 px-2.5 py-1 rounded-full uppercase tracking-wider">
                      India vs Global Outlook
                    </span>
                  </div>

                  <div className="h-[300px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart
                        data={forecast.trendData || []}
                        margin={{ top: 10, right: 10, left: -20, bottom: 0 }}
                      >
                        <defs>
                          <linearGradient id="colorIndia" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="#6366f1" stopOpacity={0.2}/>
                            <stop offset="95%" stopColor="#6366f1" stopOpacity={0}/>
                          </linearGradient>
                          <linearGradient id="colorGlobal" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="#06b6d4" stopOpacity={0.2}/>
                            <stop offset="95%" stopColor="#06b6d4" stopOpacity={0}/>
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
                        <XAxis dataKey="year" stroke={labelColor} fontSize={11} />
                        <YAxis 
                          stroke={labelColor} 
                          fontSize={11} 
                          tickFormatter={(val) => `₹${(val / 100000).toFixed(0)}L`}
                        />
                        <Tooltip 
                          contentStyle={tooltipStyle}
                          formatter={(value) => [formatINR(value), '']}
                          labelStyle={tooltipLabelStyle}
                        />
                        <Legend wrapperStyle={{ fontSize: '11px', paddingTop: '10px' }} />
                        <Area 
                          name="India Market Path" 
                          type="monotone" 
                          dataKey="salaryIndia" 
                          stroke="#6366f1" 
                          strokeWidth={2}
                          fillOpacity={1} 
                          fill="url(#colorIndia)" 
                        />
                        <Area 
                          name="Global Market Path" 
                          type="monotone" 
                          dataKey="salaryGlobal" 
                          stroke="#06b6d4" 
                          strokeWidth={2}
                          fillOpacity={1} 
                          fill="url(#colorGlobal)" 
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                </div>

                {/* Additional Insights */}
                <div className="bg-card border border-border/80 rounded-2xl p-5 flex items-start gap-4">
                  <Info className="w-5 h-5 text-indigo-600 dark:text-indigo-400 shrink-0 mt-0.5" />
                  <div className="space-y-1">
                    <h5 className="font-bold text-xs text-foreground">Geographic & Skill Multipliers</h5>
                    <p className="text-[11px] text-muted-foreground leading-normal">
                      Geographic adjustments reflect localized cost-of-living index and demand ratios (Tier 1 applies a 1.25x weight; Global utilizes a 2.3x weight). Skill tier multipliers scale compensation directly matching expertise depth (Expert yields 1.55x).
                    </p>
                  </div>
                </div>
              </>
            )}

          </div>

        </div>

      </div>
    </div>
  );
}
