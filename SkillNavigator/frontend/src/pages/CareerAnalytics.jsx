import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { 
  Sparkles, TrendingUp, BarChart2, Compass, Award, ArrowLeft, Loader2,
  PieChart as PieIcon, Activity, Flame, HelpCircle, Users, CheckSquare
} from 'lucide-react';
import { careerGuidanceApi } from '@/services/api/careerGuidance';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import {
  ResponsiveContainer, PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, 
  CartesianGrid, Tooltip, Legend, AreaChart, Area
} from 'recharts';

const COLORS = ['#6366f1', '#06b6d4', '#10b981', '#f59e0b', '#ec4899', '#8b5cf6'];

export default function CareerAnalytics() {
  const [analytics, setAnalytics] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDark, setIsDark] = useState(document.documentElement.classList.contains('dark'));

  // Monitor theme changes on root html element
  useEffect(() => {
    const observer = new MutationObserver(() => {
      setIsDark(document.documentElement.classList.contains('dark'));
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
    return () => observer.disconnect();
  }, []);

  const fetchAnalytics = async () => {
    try {
      const data = await careerGuidanceApi.getAnalytics();
      setAnalytics(data);
    } catch (err) {
      toast.error('Failed to load guidance analytics');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchAnalytics();
  }, []);

  if (isLoading) {
    return (
      <div className="h-[calc(100vh-80px)] w-full bg-background flex flex-col items-center justify-center gap-3">
        <Loader2 className="w-10 h-10 text-primary animate-spin" />
        <p className="text-muted-foreground text-xs font-semibold">Compiling analytics trends...</p>
      </div>
    );
  }

  // Active theme properties
  const gridColor = isDark ? '#1e293b' : '#e2e8f0';
  const labelColor = isDark ? '#94a3b8' : '#475569';
  const tooltipStyle = isDark 
    ? { backgroundColor: '#0f172a', borderColor: '#1e293b', borderRadius: '12px', color: '#f8fafc' }
    : { backgroundColor: '#ffffff', borderColor: '#e2e8f0', borderRadius: '12px', color: '#0f172a' };
  const tooltipLabelStyle = { color: isDark ? '#ffffff' : '#0f172a', fontSize: '11px', fontWeight: 'bold' };

  return (
    <div className="min-h-[calc(100vh-80px)] bg-background text-foreground p-4 md:p-8 relative overflow-y-auto transition-colors duration-300">
      {/* Background neon glowing blobs (subtle) */}
      <div className="absolute top-[-10%] right-[-10%] w-[500px] h-[500px] bg-indigo-500/5 dark:bg-indigo-500/10 rounded-full blur-[140px] pointer-events-none" />
      <div className="absolute bottom-[-10%] left-[-5%] w-[400px] h-[400px] bg-cyan-500/5 dark:bg-cyan-500/10 rounded-full blur-[120px] pointer-events-none" />

      <div className="max-w-5xl mx-auto space-y-6 relative z-10">
        
        {/* Navigation Breadcrumb */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <Button asChild variant="ghost" className="text-muted-foreground hover:text-foreground pl-0 text-xs gap-1.5 mb-2 sm:mb-0">
              <Link to="/career-explorer">
                <ArrowLeft className="w-4 h-4" /> Back to Mind Map
              </Link>
            </Button>
            <h1 className="text-2xl md:text-3xl font-black text-foreground flex items-center gap-2">
              <BarChart2 className="w-6 h-6 text-primary" /> Career Analytics Dashboard
            </h1>
            <p className="text-xs text-muted-foreground mt-0.5">Aggregated metrics, user choices, stream trends, and AI prediction indexes</p>
          </div>
        </div>

        {analytics && (
          <>
            {/* Top Counters Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              
              {/* Completions */}
              <div className="bg-card border border-border p-5 rounded-2xl flex items-center gap-4 shadow-soft text-card-foreground hover:border-primary/45 transition-colors duration-300">
                <div className="p-3 rounded-xl bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border border-indigo-500/20">
                  <Users className="w-6 h-6" />
                </div>
                <div>
                  <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-wider">Total Completions</span>
                  <h4 className="text-2xl font-black text-foreground mt-0.5">{analytics.totalQuizCompletions}</h4>
                  <p className="text-[9px] text-muted-foreground">Active student discovery submissions</p>
                </div>
              </div>

              {/* Confidence */}
              <div className="bg-card border border-border p-5 rounded-2xl flex items-center gap-4 shadow-soft text-card-foreground hover:border-primary/45 transition-colors duration-300">
                <div className="p-3 rounded-xl bg-cyan-500/10 text-cyan-600 dark:text-cyan-400 border border-cyan-500/20">
                  <Flame className="w-6 h-6" />
                </div>
                <div>
                  <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-wider">Model Confidence</span>
                  <h4 className="text-2xl font-black text-foreground mt-0.5">{analytics.averagePredictionConfidence}%</h4>
                  <p className="text-[9px] text-muted-foreground">Average alignment accuracy</p>
                </div>
              </div>

              {/* Top Stream */}
              <div className="bg-card border border-border p-5 rounded-2xl flex items-center gap-4 shadow-soft text-card-foreground hover:border-primary/45 transition-colors duration-300">
                <div className="p-3 rounded-xl bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                  <Activity className="w-6 h-6" />
                </div>
                <div>
                  <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-wider">Primary Stream</span>
                  <h4 className="text-base font-black text-foreground mt-1.5 leading-tight">
                    {analytics.popularStreams?.[0]?.name || 'Science'}
                  </h4>
                  <p className="text-[9px] text-muted-foreground mt-0.5">Most explored academic level</p>
                </div>
              </div>

            </div>

            {/* Middle Grid: Popular Streams (Pie) & Most Chosen Careers (Bar) */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              {/* Popular Streams Pie Chart */}
              <div className="bg-card border border-border rounded-2xl p-6 shadow-soft text-card-foreground flex flex-col justify-between">
                <div>
                  <h3 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border pb-3 mb-4">
                    <PieIcon className="w-4.5 h-4.5 text-primary" /> Popular Streams Distribution
                  </h3>
                  <div className="h-[230px] w-full relative flex items-center justify-center">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={analytics.popularStreams || []}
                          cx="50%"
                          cy="50%"
                          innerRadius={60}
                          outerRadius={90}
                          paddingAngle={4}
                          dataKey="value"
                        >
                          {(analytics.popularStreams || []).map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                          ))}
                        </Pie>
                        <Tooltip 
                          contentStyle={tooltipStyle}
                          labelStyle={tooltipLabelStyle}
                        />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                </div>

                {/* Legend labels */}
                <div className="flex flex-wrap gap-x-4 gap-y-2 justify-center pt-2">
                  {(analytics.popularStreams || []).map((entry, idx) => (
                    <div key={idx} className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <span className="w-3 h-3 rounded-full shrink-0" style={{ backgroundColor: COLORS[idx % COLORS.length] }} />
                      <span>{entry.name}: <strong className="text-foreground">{entry.value}</strong></span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Most Chosen Careers Bar Chart */}
              <div className="bg-card border border-border rounded-2xl p-6 shadow-soft text-card-foreground">
                <h3 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border pb-3 mb-4">
                  <TrendingUp className="w-4.5 h-4.5 text-cyan-500" /> Top Chosen Careers
                </h3>
                <div className="h-[250px] w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart
                      data={analytics.mostChosenCareers || []}
                      margin={{ top: 10, right: 10, left: -20, bottom: 0 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
                      <XAxis dataKey="name" stroke={labelColor} fontSize={10} tickLine={false} />
                      <YAxis stroke={labelColor} fontSize={11} allowDecimals={false} />
                      <Tooltip 
                        contentStyle={tooltipStyle}
                        labelStyle={tooltipLabelStyle}
                      />
                      <Bar dataKey="value" fill="#6366f1" radius={[4, 4, 0, 0]}>
                        {(analytics.mostChosenCareers || []).map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </div>

            </div>

            {/* Bottom Grid: Monthly Interest Trends (Area) & Skill Gap Index (Radar) */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              
              {/* Interest Trends Area Chart */}
              <div className="bg-card border border-border rounded-2xl p-6 shadow-soft text-card-foreground lg:col-span-2">
                <h3 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border pb-3 mb-4">
                  <Activity className="w-4.5 h-4.5 text-rose-500" /> Monthly User Interest Trends
                </h3>
                <div className="h-[230px] w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart
                      data={analytics.userInterestTrends || []}
                      margin={{ top: 10, right: 10, left: -25, bottom: 0 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
                      <XAxis dataKey="month" stroke={labelColor} fontSize={11} />
                      <YAxis stroke={labelColor} fontSize={11} />
                      <Tooltip contentStyle={tooltipStyle} />
                      <Legend wrapperStyle={{ fontSize: '11px', paddingTop: '5px' }} />
                      <Area name="Technology" type="monotone" dataKey="techCount" stroke="#6366f1" fill="#6366f1" fillOpacity={isDark ? 0.05 : 0.15} />
                      <Area name="Business" type="monotone" dataKey="businessCount" stroke="#f59e0b" fill="#f59e0b" fillOpacity={isDark ? 0.05 : 0.15} />
                      <Area name="Creative & Arts" type="monotone" dataKey="artsCount" stroke="#10b981" fill="#10b981" fillOpacity={isDark ? 0.05 : 0.15} />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Skill Gap Frequency Heatmap/List */}
              <div className="bg-card border border-border rounded-2xl p-6 shadow-soft text-card-foreground">
                <h3 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border pb-3 mb-4">
                  <Compass className="w-4.5 h-4.5 text-amber-500" /> Critical Skill Gaps
                </h3>
                <div className="space-y-3">
                  {(analytics.skillGapTrends || []).map((s, idx) => (
                    <div key={idx} className="p-3 rounded-xl bg-background border border-border flex items-center justify-between gap-4 hover:border-primary/30 transition-colors duration-200">
                      <div className="space-y-0.5">
                        <p className="text-xs font-bold text-foreground max-w-[170px] truncate">{s.name}</p>
                        <p className="text-[9px] text-muted-foreground">Occurs in {s.value} profiles</p>
                      </div>
                      
                      <div className="w-16 bg-secondary h-2 rounded-full overflow-hidden shrink-0">
                        <div 
                          className="h-full bg-gradient-to-r from-indigo-500 to-rose-500"
                          style={{ width: `${Math.min((s.value / 50) * 100, 100)}%` }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>

            </div>
          </>
        )}

      </div>
    </div>
  );
}
