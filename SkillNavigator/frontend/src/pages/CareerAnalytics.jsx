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
  CartesianGrid, Tooltip, Legend, AreaChart, Area, RadarChart, PolarGrid, 
  PolarAngleAxis, PolarRadiusAxis, Radar
} from 'recharts';

const COLORS = ['#6366f1', '#06b6d4', '#10b981', '#f59e0b', '#ec4899', '#8b5cf6'];

export default function CareerAnalytics() {
  const [analytics, setAnalytics] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

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
      <div className="h-[calc(100vh-80px)] w-full bg-slate-950 flex flex-col items-center justify-center gap-3">
        <Loader2 className="w-10 h-10 text-indigo-500 animate-spin" />
        <p className="text-slate-400 text-xs font-semibold">Compiling analytics trends...</p>
      </div>
    );
  }

  return (
    <div className="min-h-[calc(100vh-80px)] bg-slate-950 text-white p-4 md:p-8 relative overflow-y-auto">
      {/* Background neon glowing blobs */}
      <div className="absolute top-[-10%] right-[-10%] w-[500px] h-[500px] bg-indigo-500/10 rounded-full blur-[140px] pointer-events-none" />
      <div className="absolute bottom-[-10%] left-[-5%] w-[400px] h-[400px] bg-cyan-500/10 rounded-full blur-[120px] pointer-events-none" />

      <div className="max-w-5xl mx-auto space-y-6 relative z-10">
        
        {/* Navigation Breadcrumb */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <Button asChild variant="ghost" className="text-slate-400 hover:text-white pl-0 text-xs gap-1.5 mb-2 sm:mb-0">
              <Link to="/career-explorer">
                <ArrowLeft className="w-4 h-4" /> Back to Mind Map
              </Link>
            </Button>
            <h1 className="text-2xl md:text-3xl font-black text-white flex items-center gap-2">
              <BarChart2 className="w-6 h-6 text-indigo-400" /> Career Analytics Dashboard
            </h1>
            <p className="text-xs text-slate-400 mt-0.5">Aggregated metrics, user choices, stream trends, and AI prediction indexes</p>
          </div>
        </div>

        {analytics && (
          <>
            {/* Top Counters Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              
              {/* Completions */}
              <div className="bg-slate-900/40 border border-slate-800/80 p-5 rounded-2xl flex items-center gap-4 shadow-md">
                <div className="p-3 rounded-xl bg-indigo-950/40 text-indigo-400 border border-indigo-500/20">
                  <Users className="w-6 h-6" />
                </div>
                <div>
                  <span className="text-[10px] text-slate-500 uppercase font-bold">Total Completions</span>
                  <h4 className="text-2xl font-black text-white mt-0.5">{analytics.totalQuizCompletions}</h4>
                  <p className="text-[9px] text-slate-500">Active student discovery submissions</p>
                </div>
              </div>

              {/* Confidence */}
              <div className="bg-slate-900/40 border border-slate-800/80 p-5 rounded-2xl flex items-center gap-4 shadow-md">
                <div className="p-3 rounded-xl bg-cyan-950/40 text-cyan-400 border border-cyan-500/20">
                  <Flame className="w-6 h-6" />
                </div>
                <div>
                  <span className="text-[10px] text-slate-500 uppercase font-bold">Model Confidence</span>
                  <h4 className="text-2xl font-black text-white mt-0.5">{analytics.averagePredictionConfidence}%</h4>
                  <p className="text-[9px] text-slate-500">Average alignment accuracy</p>
                </div>
              </div>

              {/* Top Stream */}
              <div className="bg-slate-900/40 border border-slate-800/80 p-5 rounded-2xl flex items-center gap-4 shadow-md">
                <div className="p-3 rounded-xl bg-emerald-950/40 text-emerald-400 border border-emerald-500/20">
                  <Activity className="w-6 h-6" />
                </div>
                <div>
                  <span className="text-[10px] text-slate-500 uppercase font-bold">Primary Stream</span>
                  <h4 className="text-base font-black text-white mt-1 leading-tight">
                    {analytics.popularStreams?.[0]?.name || 'Science'}
                  </h4>
                  <p className="text-[9px] text-slate-500 mt-0.5">Most explored academic level</p>
                </div>
              </div>

            </div>

            {/* Middle Grid: Popular Streams (Pie) & Most Chosen Careers (Bar) */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              {/* Popular Streams Pie Chart */}
              <div className="bg-slate-900/40 border border-slate-800/80 rounded-2xl p-6 shadow-md flex flex-col justify-between">
                <div>
                  <h3 className="font-bold text-sm text-white flex items-center gap-2 border-b border-slate-800/60 pb-3 mb-4">
                    <PieIcon className="w-4.5 h-4.5 text-indigo-400" /> Popular Streams Distribution
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
                          contentStyle={{ backgroundColor: '#0f172a', borderColor: '#334155', borderRadius: '12px' }}
                          labelStyle={{ color: '#fff' }}
                        />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                </div>

                {/* Legend labels */}
                <div className="flex flex-wrap gap-x-4 gap-y-2 justify-center pt-2">
                  {(analytics.popularStreams || []).map((entry, idx) => (
                    <div key={idx} className="flex items-center gap-1.5 text-xs text-slate-400">
                      <span className="w-3 h-3 rounded-full shrink-0" style={{ backgroundColor: COLORS[idx % COLORS.length] }} />
                      <span>{entry.name}: <strong className="text-white">{entry.value}</strong></span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Most Chosen Careers Bar Chart */}
              <div className="bg-slate-900/40 border border-slate-800/80 rounded-2xl p-6 shadow-md">
                <h3 className="font-bold text-sm text-white flex items-center gap-2 border-b border-slate-800/60 pb-3 mb-4">
                  <TrendingUp className="w-4.5 h-4.5 text-cyan-400" /> Top Chosen Careers
                </h3>
                <div className="h-[250px] w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart
                      data={analytics.mostChosenCareers || []}
                      margin={{ top: 10, right: 10, left: -20, bottom: 0 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
                      <XAxis dataKey="name" stroke="#64748b" fontSize={10} tickLine={false} />
                      <YAxis stroke="#64748b" fontSize={11} allowDecimals={false} />
                      <Tooltip 
                        contentStyle={{ backgroundColor: '#0f172a', borderColor: '#334155', borderRadius: '12px' }}
                        labelStyle={{ color: '#fff', fontSize: '11px', fontWeight: 'bold' }}
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
              <div className="bg-slate-900/40 border border-slate-800/80 rounded-2xl p-6 shadow-md lg:col-span-2">
                <h3 className="font-bold text-sm text-white flex items-center gap-2 border-b border-slate-800/60 pb-3 mb-4">
                  <Activity className="w-4.5 h-4.5 text-rose-400" /> Monthly User Interest Trends
                </h3>
                <div className="h-[230px] w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart
                      data={analytics.userInterestTrends || []}
                      margin={{ top: 10, right: 10, left: -25, bottom: 0 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
                      <XAxis dataKey="month" stroke="#64748b" fontSize={11} />
                      <YAxis stroke="#64748b" fontSize={11} />
                      <Tooltip 
                        contentStyle={{ backgroundColor: '#0f172a', borderColor: '#334155', borderRadius: '12px' }}
                      />
                      <Legend wrapperStyle={{ fontSize: '11px', paddingTop: '5px' }} />
                      <Area name="Technology" type="monotone" dataKey="techCount" stroke="#6366f1" fill="#6366f1" fillOpacity={0.05} />
                      <Area name="Business" type="monotone" dataKey="businessCount" stroke="#f59e0b" fill="#f59e0b" fillOpacity={0.05} />
                      <Area name="Creative & Arts" type="monotone" dataKey="artsCount" stroke="#10b981" fill="#10b981" fillOpacity={0.05} />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Skill Gap Frequency Heatmap/List */}
              <div className="bg-slate-900/40 border border-slate-800/80 rounded-2xl p-6 shadow-md">
                <h3 className="font-bold text-sm text-white flex items-center gap-2 border-b border-slate-800/60 pb-3 mb-4">
                  <Compass className="w-4.5 h-4.5 text-amber-400" /> Critical Skill Gaps
                </h3>
                <div className="space-y-3">
                  {(analytics.skillGapTrends || []).map((s, idx) => (
                    <div key={idx} className="p-2.5 rounded-xl bg-slate-950 border border-slate-800/80 flex items-center justify-between gap-4">
                      <div className="space-y-0.5">
                        <p className="text-xs font-bold text-white max-w-[170px] truncate">{s.name}</p>
                        <p className="text-[9px] text-slate-500">Occurs in {s.value} profiles</p>
                      </div>
                      
                      <div className="w-16 bg-slate-900 h-2 rounded-full overflow-hidden shrink-0">
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
