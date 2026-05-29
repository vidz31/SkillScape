import React, { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Sparkles, CheckCircle2, ChevronRight, HelpCircle, Loader2, ArrowRight, 
  FlaskConical, Landmark, Palette, GraduationCap, Briefcase, RefreshCw, 
  Trophy, BookOpen, AlertCircle, Bookmark, Star, Wallet, Compass,
  TrendingUp, ShieldAlert, Award, School, Target, Zap, LayoutGrid, BarChart3, 
  LineChart as LineChartIcon, ThumbsUp, HelpCircle as HelpIcon, ChevronDown, ChevronUp
} from 'lucide-react';
import { ResponsiveContainer, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';
import { careerGuidanceApi } from '@/services/api/careerGuidance';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import confetti from 'canvas-confetti';

const STAGE_METADATA = {
  After10th: { title: 'After 10th Grade', desc: 'Select streams, diplomas, or vocational training paths', color: 'from-blue-500 to-indigo-500', icon: <BookOpen className="w-6 h-6" /> },
  After12th: { title: 'After 12th Grade', desc: 'Select undergraduate degrees, engineering, arts, or defence streams', color: 'from-purple-500 to-indigo-500', icon: <GraduationCap className="w-6 h-6" /> },
  AfterGraduation: { title: 'After Graduation', desc: 'Select specialized fields, industry domains, and job role routes', color: 'from-emerald-500 to-teal-500', icon: <Trophy className="w-6 h-6" /> },
  AfterPostGraduation: { title: 'After Post Graduation', desc: 'Navigate advanced academic research, PhD pathways, or professional practices', color: 'from-cyan-500 to-blue-500', icon: <School className="w-6 h-6" /> },
  CareerSwitch: { title: 'Career Switch', desc: 'Transition your career into software development, product management, or design', color: 'from-amber-500 to-orange-500', icon: <RefreshCw className="w-6 h-6" /> },
  Upskilling: { title: 'Upskilling & Certifications', desc: 'Upgrade your knowledge in AI, Cloud Computing, or Cybersecurity', color: 'from-pink-500 to-rose-500', icon: <Sparkles className="w-6 h-6" /> },
  Entrepreneurship: { title: 'Entrepreneurship & Startups', desc: 'Validate ideas, build tech startups, or launch D2C consumer brands', color: 'from-indigo-500 to-violet-500', icon: <Target className="w-6 h-6" /> },
  GovernmentJobs: { title: 'Government Jobs', desc: 'Target IAS, Civil Services, Banking PO, or Defence Officer commissions', color: 'from-red-500 to-orange-500', icon: <Landmark className="w-6 h-6" /> },
  InternationalEducation: { title: 'International Education', desc: 'Plan global MS degrees, STEM tracks, or MBA admissions abroad', color: 'from-fuchsia-500 to-pink-500', icon: <Compass className="w-6 h-6" /> }
};

// Branching logic to construct the active quiz pathway based on user answers
function constructQuizPath(allQuestions, answers) {
  if (!allQuestions || allQuestions.length === 0) {
    return { path: [], accumulatedWeights: {} };
  }

  // 1. Level 1 questions are always included
  const level1 = allQuestions.filter(q => q.hierarchyLevel === 1 || q.HierarchyLevel === 1);
  let path = [...level1];

  let currentLevel = 1;
  let accumulatedWeights = {};

  while (currentLevel <= 5) {
    const levelQuestions = path.filter(q => q.hierarchyLevel === currentLevel || q.HierarchyLevel === currentLevel);
    if (levelQuestions.length === 0) break;

    // Check if all questions for this level in the current path are answered
    let allLevelAnswered = true;
    let chosenNextTags = [];

    for (const q of levelQuestions) {
      const ans = answers.find(a => a.questionId === q.id || a.QuestionId === q.id);
      if (!ans) {
        allLevelAnswered = false;
        break;
      }

      const optIdx = ans.optionIndex !== undefined ? ans.optionIndex : ans.OptionIndex;
      const option = q.options ? q.options[optIdx] : (q.Options ? q.Options[optIdx] : null);
      if (option) {
        // Accumulate weights
        const weights = option.weights || option.Weights || {};
        Object.entries(weights).forEach(([tag, w]) => {
          accumulatedWeights[tag.toLowerCase()] = (accumulatedWeights[tag.toLowerCase()] || 0) + w;
        });

        // Track explicit next tag pointers
        const nextTags = option.nextLevelTags || option.NextLevelTags;
        if (nextTags) {
          chosenNextTags.push(nextTags.toLowerCase());
        }
      }
    }

    if (!allLevelAnswered) {
      break;
    }

    let nextLevel = currentLevel + 1;
    if (nextLevel > 5) break;

    // Find the tag to branch into for the next level
    let nextTag = null;
    if (chosenNextTags.length > 0) {
      // Find the most frequent next tag chosen at this level
      const freq = {};
      chosenNextTags.forEach(t => {
        freq[t] = (freq[t] || 0) + 1;
      });
      nextTag = Object.keys(freq).reduce((a, b) => freq[a] >= freq[b] ? a : b);
    }

    if (!nextTag) {
      // Fallback: search accumulated weights for the highest weight
      let maxW = -1;
      Object.entries(accumulatedWeights).forEach(([tag, w]) => {
        if (w > maxW) {
          maxW = w;
          nextTag = tag;
        }
      });
    }

    if (nextTag) {
      // Find questions at nextLevel that target this tag
      const nextLevelQuestions = allQuestions.filter(q => {
        const lvl = q.hierarchyLevel !== undefined ? q.hierarchyLevel : q.HierarchyLevel;
        if (lvl !== nextLevel) return false;
        const targetTagsStr = q.targetCareerTags || q.TargetCareerTags || "";
        const tags = targetTagsStr.toLowerCase().split(',').map(t => t.trim());
        return tags.includes(nextTag.toLowerCase());
      });

      if (nextLevelQuestions.length > 0) {
        path.push(...nextLevelQuestions);
      } else {
        // Fallback: push the first question of that level
        const fallback = allQuestions.find(q => {
          const lvl = q.hierarchyLevel !== undefined ? q.hierarchyLevel : q.HierarchyLevel;
          return lvl === nextLevel;
        });
        if (fallback) path.push(fallback);
      }
    } else {
      // Fallback: push the first question of that level
      const fallback = allQuestions.find(q => {
        const lvl = q.hierarchyLevel !== undefined ? q.hierarchyLevel : q.HierarchyLevel;
        return lvl === nextLevel;
      });
      if (fallback) path.push(fallback);
    }

    currentLevel++;
  }

  return { path, accumulatedWeights };
}

export default function QuizEngine() {
  const [step, setStep] = useState('landing'); // landing, quiz, loading, result
  const [selectedStream, setSelectedStream] = useState('');
  const [questions, setQuestions] = useState([]);
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [answers, setAnswers] = useState([]); // Array of { questionId, optionIndex }
  const [isLoading, setIsLoading] = useState(false);
  const [resultProfile, setResultProfile] = useState(null);
  const [bookmarked, setBookmarked] = useState(false);

  // Result view state
  const [activeResultTab, setActiveResultTab] = useState('overview'); // overview, matches, strengths, salary
  const [expandedMatchId, setExpandedMatchId] = useState(null);

  // Compute active question pathway dynamically
  const { path: activeQuestions, accumulatedWeights } = useMemo(() => {
    return constructQuizPath(questions, answers);
  }, [questions, answers]);

  // Trigger quiz start
  const handleStartQuiz = async (stream) => {
    setSelectedStream(stream);
    setIsLoading(true);
    setStep('quiz');
    try {
      const qData = await careerGuidanceApi.getQuizQuestions(stream);
      if (!qData || qData.length === 0) {
        toast.error('No quiz questions configured for this stage yet.');
        setStep('landing');
      } else {
        setQuestions(qData);
        setCurrentQuestionIndex(0);
        setAnswers([]);
      }
    } catch (err) {
      toast.error('Failed to load quiz questions.');
      setStep('landing');
    } finally {
      setIsLoading(false);
    }
  };

  // Option select
  const handleSelectOption = (optionIndex) => {
    const newAnswers = answers.slice(0, currentQuestionIndex);
    newAnswers[currentQuestionIndex] = {
      questionId: activeQuestions[currentQuestionIndex].id,
      optionIndex
    };
    setAnswers(newAnswers);

    // Calculate next path to check if quiz is complete
    const nextPathInfo = constructQuizPath(questions, newAnswers);
    const nextActiveQuestions = nextPathInfo.path;

    // Auto proceed with short timeout for UX
    setTimeout(() => {
      if (currentQuestionIndex < nextActiveQuestions.length - 1) {
        setCurrentQuestionIndex(currentQuestionIndex + 1);
      } else {
        handleSubmitQuiz(newAnswers);
      }
    }, 250);
  };

  // Submit quiz answers to backend
  const handleSubmitQuiz = async (finalAnswers) => {
    setStep('loading');
    try {
      const payload = {
        selectedStream,
        answers: finalAnswers
      };
      const result = await careerGuidanceApi.submitQuizAnswers(payload);
      setResultProfile(result);
      setBookmarked(result.bookmarkedPaths?.includes(result.recommendedCareer?.id));
      
      // Deliberate slight delay to allow loading animations to look AI-like
      setTimeout(() => {
        setStep('result');
        confetti({
          particleCount: 150,
          spread: 80,
          origin: { y: 0.6 }
        });
      }, 1500);
    } catch (err) {
      toast.error('Failed to submit quiz responses.');
      setStep('quiz');
    }
  };

  // Toggle Bookmark
  const handleToggleBookmark = async () => {
    if (!resultProfile?.recommendedCareer) return;
    try {
      const isBookmarked = await careerGuidanceApi.toggleBookmark(resultProfile.recommendedCareer.id);
      setBookmarked(isBookmarked);
      toast.success(isBookmarked ? 'Career path saved to bookmarks' : 'Removed from bookmarks');
    } catch (err) {
      toast.error('Failed to toggle bookmark');
    }
  };

  // Reset quiz
  const handleReset = () => {
    setStep('landing');
    setSelectedStream('');
    setQuestions([]);
    setCurrentQuestionIndex(0);
    setAnswers([]);
    setResultProfile(null);
    setActiveResultTab('overview');
    setExpandedMatchId(null);
  };

  // Trajectory chart calculation
  const salaryChartData = useMemo(() => {
    if (!resultProfile || !resultProfile.careerGrowthGraph) return [];
    return resultProfile.careerGrowthGraph.map(point => ({
      year: point.year || point.Year,
      India: Math.round((point.salaryIndia || point.SalaryIndia) / 100000 * 10) / 10, // Lakhs
      Global: Math.round((point.salaryGlobal || point.SalaryGlobal) / 100000 * 10) / 10 // Lakhs
    }));
  }, [resultProfile]);

  // Extract Strengths and Weaknesses
  const strengths = useMemo(() => {
    if (!resultProfile?.strengthsWeaknesses) return [];
    return resultProfile.strengthsWeaknesses.Strengths || resultProfile.strengthsWeaknesses.strengths || [];
  }, [resultProfile]);

  const weaknesses = useMemo(() => {
    if (!resultProfile?.strengthsWeaknesses) return [];
    return resultProfile.strengthsWeaknesses.Weaknesses || resultProfile.strengthsWeaknesses.weaknesses || [];
  }, [resultProfile]);

  return (
    <div className="min-h-[calc(100vh-80px)] w-full bg-slate-950 text-white flex flex-col items-center justify-center p-4 relative overflow-y-auto">
      {/* Background gradients */}
      <div className="absolute top-10 left-10 w-96 h-96 bg-indigo-500/10 rounded-full blur-[100px] pointer-events-none" />
      <div className="absolute bottom-10 right-10 w-96 h-96 bg-cyan-500/10 rounded-full blur-[100px] pointer-events-none" />

      <AnimatePresence mode="wait">
        {/* STEP 1: Landing Page */}
        {step === 'landing' && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="w-full max-w-4xl text-center space-y-8 z-10 py-8"
          >
            <div className="space-y-3">
              <span className="text-xs font-semibold bg-indigo-500/10 text-indigo-400 border border-indigo-500/25 px-3 py-1 rounded-full uppercase tracking-wider">
                Interactive Career Discovery Ecosystem
              </span>
              <h1 className="text-3xl md:text-5xl font-black bg-gradient-to-r from-white via-indigo-200 to-cyan-300 bg-clip-text text-transparent leading-tight pt-1">
                Find Your Ideal Career Pathway
              </h1>
              <p className="text-slate-400 max-w-xl mx-auto text-sm md:text-base">
                Discover streams, domains, and specific roles with our progressive decision-tree adaptive quiz engine.
              </p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6 mt-8 max-w-4xl mx-auto">
              {Object.entries(STAGE_METADATA).map(([key, meta]) => (
                <button
                  key={key}
                  onClick={() => handleStartQuiz(key)}
                  className="group relative flex flex-col items-center p-6 rounded-2xl bg-slate-900/60 border border-slate-800 hover:border-indigo-500/40 transition-all duration-300 hover:scale-102 hover:shadow-lg hover:shadow-indigo-500/5 text-center overflow-hidden h-full"
                >
                  <div className={`absolute inset-0 bg-gradient-to-br ${meta.color} opacity-0 group-hover:opacity-5 transition-opacity duration-300`} />
                  
                  <div className={`p-3 rounded-xl bg-slate-950 border border-slate-800 text-indigo-400 mb-4 group-hover:scale-110 transition-transform duration-300`}>
                    {meta.icon}
                  </div>
                  <h3 className="font-bold text-white text-sm mb-1">{meta.title}</h3>
                  <p className="text-[11px] text-slate-500 group-hover:text-slate-400 transition-colors leading-normal flex-1">
                    {meta.desc}
                  </p>
                  <ChevronRight className="w-4 h-4 text-slate-500 group-hover:text-white mt-4 transition-transform duration-300 group-hover:translate-x-1" />
                </button>
              ))}
            </div>
          </motion.div>
        )}

        {/* STEP 2: Quiz Engine */}
        {step === 'quiz' && activeQuestions.length > 0 && (
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0 }}
            className="w-full max-w-2xl bg-slate-900/40 border border-slate-800/80 backdrop-blur-md rounded-2xl p-6 md:p-8 space-y-6 z-10 shadow-xl"
          >
            {/* Header / Progress */}
            <div className="flex items-center justify-between border-b border-slate-800/60 pb-4">
              <div>
                <span className="text-[10px] uppercase font-bold text-indigo-400 tracking-wider">
                  {STAGE_METADATA[selectedStream]?.title}
                </span>
                <h3 className="text-xs text-slate-400 mt-0.5">
                  Question {currentQuestionIndex + 1} of {activeQuestions.length}
                </h3>
              </div>
              <div className="flex items-center gap-1 bg-slate-950 border border-slate-800 px-3 py-1 rounded-full text-[10px] text-slate-400">
                <Compass className="w-3.5 h-3.5 text-cyan-400 animate-spin-slow animate-spin" />
                <span>Level {activeQuestions[currentQuestionIndex]?.hierarchyLevel || activeQuestions[currentQuestionIndex]?.HierarchyLevel} Analysis</span>
              </div>
            </div>

            {/* Progress Bar */}
            <div className="w-full h-1.5 bg-slate-950 rounded-full overflow-hidden">
              <motion.div
                className="h-full bg-gradient-to-r from-indigo-500 to-cyan-500"
                initial={{ width: 0 }}
                animate={{ width: `${((currentQuestionIndex + 1) / activeQuestions.length) * 100}%` }}
                transition={{ duration: 0.3 }}
              />
            </div>

            {/* Question Text */}
            <div className="space-y-4">
              <h2 className="text-lg md:text-xl font-bold text-white leading-snug">
                {activeQuestions[currentQuestionIndex]?.questionText || activeQuestions[currentQuestionIndex]?.QuestionText}
              </h2>
            </div>

            {/* Options list */}
            <div className="space-y-3 pt-2">
              {(activeQuestions[currentQuestionIndex]?.options || activeQuestions[currentQuestionIndex]?.Options || []).map((opt, idx) => {
                const isSelected = answers[currentQuestionIndex]?.optionIndex === idx;
                return (
                  <button
                    key={idx}
                    onClick={() => handleSelectOption(idx)}
                    className={`w-full text-left p-4 rounded-xl border text-sm font-semibold transition-all duration-300 flex items-center justify-between gap-4 group ${
                      isSelected
                        ? 'bg-indigo-950/30 border-indigo-500 text-white shadow-md'
                        : 'bg-slate-950/40 border-slate-800 hover:border-slate-700 text-slate-300 hover:text-white'
                    }`}
                  >
                    <span>{opt.text || opt.Text}</span>
                    <span className={`w-5 h-5 rounded-full border flex items-center justify-center shrink-0 transition-colors ${
                      isSelected ? 'border-indigo-400 bg-indigo-500 text-white' : 'border-slate-800 group-hover:border-slate-600'
                    }`}>
                      {isSelected && <CheckCircle2 className="w-3.5 h-3.5" />}
                    </span>
                  </button>
                );
              })}
            </div>

            {/* Back Button */}
            <div className="flex justify-between items-center pt-2">
              <Button
                variant="ghost"
                onClick={() => {
                  if (currentQuestionIndex > 0) {
                    setCurrentQuestionIndex(currentQuestionIndex - 1);
                  } else {
                    handleReset();
                  }
                }}
                className="text-xs text-slate-400 hover:text-white"
              >
                Back
              </Button>
              <span className="text-[10px] text-slate-500">Adaptive Decision Tree System</span>
            </div>
          </motion.div>
        )}

        {/* STEP 3: Loading calculations */}
        {step === 'loading' && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="text-center space-y-4 z-10"
          >
            <div className="relative w-16 h-16 mx-auto">
              <Loader2 className="w-16 h-16 text-indigo-500 animate-spin" />
              <Sparkles className="w-6 h-6 text-cyan-400 absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2" />
            </div>
            <div className="space-y-1">
              <h3 className="font-bold text-lg text-white">Analyzing Your Inputs</h3>
              <p className="text-xs text-slate-400 max-w-xs mx-auto">
                Running scoring models against the {selectedStream} career hierarchy...
              </p>
            </div>
          </motion.div>
        )}

        {/* STEP 4: Results Display */}
        {step === 'result' && resultProfile && (
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            className="w-full max-w-4xl space-y-6 z-10 py-6"
          >
            {/* Header Result Card */}
            <div className="p-6 md:p-8 rounded-2xl bg-gradient-to-b from-indigo-950/40 to-slate-900/40 border border-indigo-500/30 backdrop-blur-md shadow-2xl relative overflow-hidden">
              <div className="absolute top-0 right-0 w-64 h-64 bg-indigo-500/10 rounded-full blur-[80px]" />
              
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 relative z-10">
                <div className="space-y-2">
                  <span className="text-xs font-bold uppercase bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 px-3 py-1 rounded-full tracking-wider">
                    Recommended Match Profile
                  </span>
                  <h1 className="text-2xl md:text-3xl font-black text-white leading-tight">
                    {resultProfile.recommendedCareer?.title}
                  </h1>
                  <p className="text-xs text-slate-400 max-w-xl leading-normal">
                    {resultProfile.recommendedCareer?.description}
                  </p>
                </div>

                <div className="flex items-center gap-4 shrink-0 bg-slate-950/80 border border-slate-800/80 p-4 rounded-xl">
                  <div className="text-center">
                    <p className="text-[10px] text-slate-500 uppercase font-bold">Confidence</p>
                    <p className="text-2xl font-black text-indigo-400">{resultProfile.confidenceScore?.toFixed(1)}%</p>
                  </div>
                  <div className="h-8 w-px bg-slate-800" />
                  <div className="text-center">
                    <p className="text-[10px] text-slate-500 uppercase font-bold">Est. Salary</p>
                    <p className="text-2xl font-black text-emerald-400">
                      ₹{(resultProfile.predictedSalary / 100000).toFixed(1)}L+
                    </p>
                  </div>
                </div>
              </div>

              <div className="mt-6 pt-6 border-t border-slate-800/80 flex flex-wrap gap-3 items-center justify-between">
                <div className="flex items-center gap-1.5">
                  <span className="text-xs text-slate-400">Target Skills Needed:</span>
                  <div className="flex flex-wrap gap-1.5">
                    {resultProfile.skillGap?.slice(0, 3).map((s, idx) => (
                      <span key={idx} className="text-[11px] bg-slate-950 text-indigo-400 border border-indigo-500/20 px-2.5 py-0.5 rounded-full font-medium">
                        {s}
                      </span>
                    ))}
                    {resultProfile.skillGap?.length > 3 && (
                      <span className="text-[10px] text-slate-500 font-semibold mt-0.5">+{resultProfile.skillGap.length - 3} more</span>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <Button
                    onClick={handleToggleBookmark}
                    variant="outline"
                    className="border-slate-800 bg-slate-950/80 hover:bg-slate-900 text-xs gap-1.5 text-slate-300 h-8"
                  >
                    <Bookmark className={`w-4 h-4 ${bookmarked ? 'fill-indigo-400 text-indigo-400' : 'text-slate-400'}`} />
                    {bookmarked ? 'Saved' : 'Bookmark'}
                  </Button>
                  <Button
                    onClick={handleReset}
                    variant="ghost"
                    className="text-slate-400 hover:text-white hover:bg-slate-900/60 text-xs h-8"
                  >
                    <RefreshCw className="w-3.5 h-3.5 mr-1" /> Retake Quiz
                  </Button>
                </div>
              </div>
            </div>

            {/* Tabs Header */}
            <div className="flex border-b border-slate-800 gap-6 bg-slate-900/10 p-1.5 rounded-xl border border-slate-800/40">
              <button 
                onClick={() => setActiveResultTab('overview')}
                className={`px-4 py-2 text-xs font-bold transition-all rounded-lg flex items-center gap-1.5 ${
                  activeResultTab === 'overview' 
                    ? 'bg-slate-900 text-indigo-400 border border-slate-800' 
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                <LayoutGrid className="w-4 h-4" /> Overview & Risks
              </button>
              <button 
                onClick={() => setActiveResultTab('matches')}
                className={`px-4 py-2 text-xs font-bold transition-all rounded-lg flex items-center gap-1.5 ${
                  activeResultTab === 'matches' 
                    ? 'bg-slate-900 text-indigo-400 border border-slate-800' 
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                <Trophy className="w-4 h-4" /> Top 10 Matches
              </button>
              <button 
                onClick={() => setActiveResultTab('strengths')}
                className={`px-4 py-2 text-xs font-bold transition-all rounded-lg flex items-center gap-1.5 ${
                  activeResultTab === 'strengths' 
                    ? 'bg-slate-900 text-indigo-400 border border-slate-800' 
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                <Target className="w-4 h-4" /> Personality & Roadmap
              </button>
              <button 
                onClick={() => setActiveResultTab('salary')}
                className={`px-4 py-2 text-xs font-bold transition-all rounded-lg flex items-center gap-1.5 ${
                  activeResultTab === 'salary' 
                    ? 'bg-slate-900 text-indigo-400 border border-slate-800' 
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                <LineChartIcon className="w-4 h-4" /> 10-Year Salary Forecast
              </button>
            </div>

            {/* TAB CONTENTS */}
            <div className="min-h-[300px]">
              {/* TAB 1: OVERVIEW & RISKS */}
              {activeResultTab === 'overview' && (
                <div className="space-y-4 animate-in fade-in duration-300">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Why Recommended Card */}
                    <div className="p-5 rounded-xl border border-emerald-500/20 bg-emerald-950/10 space-y-2">
                      <h4 className="text-xs font-bold uppercase tracking-wider text-emerald-400 flex items-center gap-1.5">
                        <ThumbsUp className="w-4 h-4" /> Why This Fits You (Pros)
                      </h4>
                      <p className="text-xs text-slate-300 leading-relaxed">
                        {resultProfile.whyRecommended || 'Your answers reveal strong quantitative skills, creative conceptualization, and structured thinking style align with this role\'s requirements.'}
                      </p>
                    </div>

                    {/* Why Not Recommended (Cons/Challenges/Risks) */}
                    <div className="p-5 rounded-xl border border-rose-500/20 bg-rose-950/10 space-y-2">
                      <h4 className="text-xs font-bold uppercase tracking-wider text-rose-400 flex items-center gap-1.5">
                        <ShieldAlert className="w-4 h-4" /> Risks & Disadvantages (Cons)
                      </h4>
                      <p className="text-xs text-slate-300 leading-relaxed">
                        {resultProfile.whyNotRecommended || 'This path might present steep learning curves in advanced technical subjects. Consider potential automation risks or long education timeline barriers.'}
                      </p>
                    </div>
                  </div>

                  {/* Highlights Grid */}
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-2">
                    <div className="p-4 rounded-xl bg-slate-900 border border-slate-800/80">
                      <span className="text-[10px] text-slate-500 font-semibold block mb-1">Growth rate CAGR</span>
                      <span className="text-sm font-bold text-orange-400">{resultProfile.recommendedCareer?.industryGrowth}%</span>
                    </div>
                    <div className="p-4 rounded-xl bg-slate-900 border border-slate-800/80">
                      <span className="text-[10px] text-slate-500 font-semibold block mb-1">Automation Risk</span>
                      <span className={`text-sm font-bold ${resultProfile.recommendedCareer?.automationRisk >= 50.0 ? 'text-rose-400' : 'text-emerald-400'}`}>
                        {resultProfile.recommendedCareer?.automationRisk}%
                      </span>
                    </div>
                    <div className="p-4 rounded-xl bg-slate-900 border border-slate-800/80">
                      <span className="text-[10px] text-slate-500 font-semibold block mb-1">Remote Capability</span>
                      <span className="text-sm font-bold text-white">{resultProfile.recommendedCareer?.remoteWorkPossibility}%</span>
                    </div>
                    <div className="p-4 rounded-xl bg-slate-900 border border-slate-800/80">
                      <span className="text-[10px] text-slate-500 font-semibold block mb-1">Difficulty level</span>
                      <span className="text-sm font-bold text-indigo-400">{resultProfile.recommendedCareer?.difficultyLevel || 'Medium'}</span>
                    </div>
                  </div>
                </div>
              )}

              {/* TAB 2: TOP 10 MATCHES */}
              {activeResultTab === 'matches' && (
                <div className="space-y-3 animate-in fade-in duration-300">
                  <p className="text-xs text-slate-400 mb-2">Here are the top 10 career pathways matching your responses in the {STAGE_METADATA[selectedStream]?.title} category:</p>
                  {(resultProfile.topMatches || []).map((match, idx) => {
                    const matchScore = Math.max(50, Math.round(resultProfile.confidenceScore - idx * 2.5));
                    const isExpanded = expandedMatchId === match.id;
                    const maxSal = match.salaryData ? Math.max(...Object.values(match.salaryData)) : 0;
                    
                    return (
                      <div key={match.id} className="border border-slate-800 rounded-xl bg-slate-900/60 overflow-hidden">
                        {/* Summary Header */}
                        <div 
                          onClick={() => setExpandedMatchId(isExpanded ? null : match.id)}
                          className="p-4 flex items-center justify-between gap-4 cursor-pointer hover:bg-slate-900 transition-colors"
                        >
                          <div className="flex items-center gap-3">
                            <span className="w-6 h-6 rounded-full bg-slate-950 border border-slate-800 text-xs font-bold text-indigo-400 flex items-center justify-center">
                              {idx + 1}
                            </span>
                            <div>
                              <h4 className="text-xs font-bold text-white">{match.title}</h4>
                              <p className="text-[10px] text-slate-500">Level {match.level} • CAGR: {match.industryGrowth}%</p>
                            </div>
                          </div>

                          <div className="flex items-center gap-4">
                            {/* Confidence Gauge */}
                            <div className="w-24 bg-slate-950 h-2 rounded-full overflow-hidden border border-slate-800">
                              <div className="bg-indigo-500 h-full" style={{ width: `${matchScore}%` }} />
                            </div>
                            <span className="text-[10px] font-mono text-indigo-400 font-bold shrink-0">{matchScore}% Match</span>
                            {isExpanded ? <ChevronUp className="w-4 h-4 text-slate-500" /> : <ChevronDown className="w-4 h-4 text-slate-500" />}
                          </div>
                        </div>

                        {/* Collapsible Content */}
                        {isExpanded && (
                          <div className="p-4 bg-slate-950/40 border-t border-slate-800/60 space-y-3 text-xs">
                            <p className="text-slate-300 leading-normal">{match.description}</p>
                            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 pt-2 text-[10px]">
                              <div>
                                <span className="text-slate-500 block">Required Degrees:</span>
                                <span className="font-semibold text-white">{match.requiredDegrees?.join(', ') || 'N/A'}</span>
                              </div>
                              <div>
                                <span className="text-slate-500 block">Est. Max Salary:</span>
                                <span className="font-semibold text-emerald-400">₹{(maxSal/100000).toFixed(1)}L/yr</span>
                              </div>
                              <div>
                                <span className="text-slate-500 block">AI Displ. Risk:</span>
                                <span className={`font-semibold ${match.automationRisk >= 50.0 ? 'text-rose-400' : 'text-emerald-400'}`}>
                                  {match.automationRisk}%
                                </span>
                              </div>
                              <div>
                                <span className="text-slate-500 block">Environment:</span>
                                <span className="font-semibold text-white">{match.workEnvironment}</span>
                              </div>
                            </div>
                            <div className="pt-3 flex gap-2 justify-end">
                              <Button asChild size="sm" variant="outline" className="h-7 text-[10px] border-slate-800">
                                <Link to={`/career-insights?id=${match.id}`}>Explore Insights</Link>
                              </Button>
                              <Button asChild size="sm" className="h-7 text-[10px] bg-indigo-600 hover:bg-indigo-500">
                                <Link to={`/salary-forecast?id=${match.id}`}>Forecast Salary</Link>
                              </Button>
                            </div>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}

              {/* TAB 3: PERSONALITY & ROADMAP */}
              {activeResultTab === 'strengths' && (
                <div className="space-y-5 animate-in fade-in duration-300">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Strengths List */}
                    <div className="p-5 rounded-xl border border-slate-800 bg-slate-900/60 space-y-2">
                      <h4 className="text-xs font-bold uppercase tracking-wider text-emerald-400 flex items-center gap-1.5">
                        <CheckCircle2 className="w-4 h-4" /> Personal Strengths Analyzed
                      </h4>
                      {strengths.length > 0 ? (
                        <ul className="space-y-2">
                          {strengths.map((str, idx) => (
                            <li key={idx} className="text-xs text-slate-300 flex items-start gap-2">
                              <ChevronRight className="w-3.5 h-3.5 text-emerald-400 mt-0.5 shrink-0" />
                              <span>{str}</span>
                            </li>
                          ))}
                        </ul>
                      ) : (
                        <p className="text-xs text-slate-500 italic">No specific strengths mapped. Retake the quiz for higher fidelity scores.</p>
                      )}
                    </div>

                    {/* Weaknesses List */}
                    <div className="p-5 rounded-xl border border-slate-800 bg-slate-900/60 space-y-2">
                      <h4 className="text-xs font-bold uppercase tracking-wider text-rose-400 flex items-center gap-1.5">
                        <AlertCircle className="w-4 h-4" /> Aptitude Gaps & Weaknesses
                      </h4>
                      {weaknesses.length > 0 ? (
                        <ul className="space-y-2">
                          {weaknesses.map((wk, idx) => (
                            <li key={idx} className="text-xs text-slate-300 flex items-start gap-2">
                              <ChevronRight className="w-3.5 h-3.5 text-rose-400 mt-0.5 shrink-0" />
                              <span>{wk}</span>
                            </li>
                          ))}
                        </ul>
                      ) : (
                        <p className="text-xs text-slate-500 italic">No major vulnerabilities detected in this stream category.</p>
                      )}
                    </div>
                  </div>

                  {/* Learning Roadmap Preview */}
                  <div className="p-5 rounded-xl border border-slate-800 bg-slate-900/60">
                    <h3 className="font-bold text-xs text-white mb-4 flex items-center gap-2">
                      <BookOpen className="w-4 h-4 text-indigo-400" /> Sequential Learning Milestones
                    </h3>
                    <div className="space-y-4 relative before:absolute before:top-2 before:bottom-2 before:left-[11px] before:w-[2px] before:bg-slate-800">
                      {(resultProfile.roadmap || []).map((item, idx) => (
                        <div key={idx} className="flex gap-4 relative z-10">
                          <span className="w-6 h-6 rounded-full bg-slate-950 border border-slate-800 text-[11px] font-bold text-indigo-400 flex items-center justify-center shrink-0 shadow-lg">
                            {idx + 1}
                          </span>
                          <div>
                            <h4 className="text-xs font-bold text-white">{item.phase || item.Phase}</h4>
                            <p className="text-[10px] text-slate-400 mt-0.5">Duration: {item.duration || item.Duration}</p>
                            {item.topics && (
                              <div className="flex flex-wrap gap-1 mt-1.5">
                                {(item.topics || []).map((top, tIdx) => (
                                  <span key={tIdx} className="text-[9px] bg-slate-950 px-2 py-0.5 rounded text-slate-500 border border-slate-800/40">
                                    {top}
                                  </span>
                                ))}
                              </div>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}

              {/* TAB 4: SALARY TRAJECTORY */}
              {activeResultTab === 'salary' && (
                <div className="space-y-5 animate-in fade-in duration-300">
                  <div className="p-5 rounded-xl border border-slate-800 bg-slate-900/60">
                    <div className="flex justify-between items-center mb-4">
                      <div>
                        <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400">10-Year Salary Trajectory Projection</h4>
                        <p className="text-[10px] text-slate-500">Based on industry growth rate of {resultProfile.recommendedCareer?.industryGrowth}% CAGR</p>
                      </div>
                      <span className="text-xs bg-slate-950 border border-slate-800 text-emerald-400 font-semibold px-2 py-0.5 rounded">
                        Indian vs Global Comparison
                      </span>
                    </div>

                    {/* Recharts Compound Chart */}
                    {salaryChartData.length > 0 ? (
                      <div className="h-60 w-full">
                        <ResponsiveContainer width="100%" height="100%">
                          <AreaChart data={salaryChartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                            <defs>
                              <linearGradient id="colorIndia" x1="0" y1="0" x2="0" y2="1">
                                <stop offset="5%" stopColor="#22c55e" stopOpacity={0.2}/>
                                <stop offset="95%" stopColor="#22c55e" stopOpacity={0}/>
                              </linearGradient>
                              <linearGradient id="colorGlobal" x1="0" y1="0" x2="0" y2="1">
                                <stop offset="5%" stopColor="#6366f1" stopOpacity={0.2}/>
                                <stop offset="95%" stopColor="#6366f1" stopOpacity={0}/>
                              </linearGradient>
                            </defs>
                            <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
                            <XAxis dataKey="year" stroke="#64748b" fontSize={10} />
                            <YAxis stroke="#64748b" fontSize={10} unit="L" />
                            <Tooltip 
                              contentStyle={{ backgroundColor: '#0f172a', borderColor: '#334155', color: '#fff', fontSize: 11 }} 
                              formatter={(value) => [`₹${value} Lakhs`, '']}
                            />
                            <Legend wrapperStyle={{ fontSize: 10, pt: 10 }} />
                            <Area name="India Salary (Lakhs)" type="monotone" dataKey="India" stroke="#22c55e" strokeWidth={2} fillOpacity={1} fill="url(#colorIndia)" />
                            <Area name="Global Salary (Lakhs)" type="monotone" dataKey="Global" stroke="#6366f1" strokeWidth={2} fillOpacity={1} fill="url(#colorGlobal)" />
                          </AreaChart>
                        </ResponsiveContainer>
                      </div>
                    ) : (
                      <div className="h-40 flex items-center justify-center text-slate-500 italic text-xs">
                        No growth projection data available for this pathway.
                      </div>
                    )}
                  </div>

                  {/* ROI analysis block */}
                  <div className="p-5 rounded-xl border border-slate-800 bg-slate-900/60 flex flex-col md:flex-row justify-between gap-4 items-center">
                    <div className="space-y-1 text-center md:text-left">
                      <h4 className="text-xs font-bold text-white flex items-center gap-1.5 justify-center md:justify-start">
                        <Wallet className="w-4 h-4 text-emerald-400" /> ROI and Wealth Generation Rating
                      </h4>
                      <p className="text-[11px] text-slate-400 max-w-xl">
                        With an initial starting point of ₹{(resultProfile.predictedSalary / 100000).toFixed(1)}L and global multipliers up to 2.3x, this career path has a high return-on-investment rating compared to standard benchmarks.
                      </p>
                    </div>

                    <Button asChild className="bg-indigo-600 hover:bg-indigo-500 font-semibold gap-1 text-xs shrink-0 text-white border-0 h-9">
                      <Link to={`/salary-forecast?id=${resultProfile.recommendedCareer?.id}`}>
                        Run Forecast Simulator <ArrowRight className="w-3.5 h-3.5" />
                      </Link>
                    </Button>
                  </div>
                </div>
              )}
            </div>

            {/* Quick Actions Footer Card */}
            <div className="flex gap-2 flex-col sm:flex-row">
              <Button asChild className="flex-1 bg-gradient-to-r from-indigo-600 to-cyan-600 hover:from-indigo-500 hover:to-cyan-500 font-bold gap-1 text-white border-0 text-xs">
                <Link to={`/career-insights?id=${resultProfile.recommendedCareer?.id}`}>
                  View Detailed Career Insights <ArrowRight className="w-4 h-4" />
                </Link>
              </Button>
              <Button asChild variant="outline" className="border-slate-800 bg-slate-950/80 hover:bg-slate-900 text-xs text-slate-300">
                <Link to="/career-explorer">
                  Navigate interactive Mind Map
                </Link>
              </Button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
