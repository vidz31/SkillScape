import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { placementService } from '@/services/api/placement';
import {
  Trophy, BookOpen, BrainCircuit, Target, Briefcase,
  ArrowRight, CheckCircle2, ChevronRight, Check, X, Code2, Layers, Cloud
} from 'lucide-react';

const PlacementReadiness = () => {
  const [stage, setStage] = useState('role-selection'); // role-selection | test-taking | result

  // Data states
  const [roles, setRoles] = useState([]);
  const [selectedRole, setSelectedRole] = useState(null);
  const [questions, setQuestions] = useState([]);
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [answers, setAnswers] = useState([]);
  const [result, setResult] = useState(null);

  // Loading states
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    fetchRoles();
  }, []);

  const fetchRoles = async () => {
    try {
      setLoading(true);
      const data = await placementService.getAvailableRoles();
      setRoles(data);
    } catch (error) {
      toast.error('Failed to load available roles');
    } finally {
      setLoading(false);
    }
  };

  const startTest = async (roleName) => {
    try {
      setLoading(true);
      setSelectedRole(roleName);
      const data = await placementService.getAssessmentQuestions(roleName);
      setQuestions(data);
      setAnswers([]);
      setCurrentQuestionIndex(0);
      setStage('test-taking');
    } catch (error) {
      toast.error('Failed to load assessment questions');
    } finally {
      setLoading(false);
    }
  };

  const handleAnswerSelect = (option) => {
    const newAnswers = [...answers];
    newAnswers[currentQuestionIndex] = {
      questionId: questions[currentQuestionIndex].id,
      selectedAnswer: option
    };
    setAnswers(newAnswers);
  };

  const handleNext = () => {
    if (!answers[currentQuestionIndex]) {
      toast.error('Please select an answer to continue');
      return;
    }

    if (currentQuestionIndex < questions.length - 1) {
      setCurrentQuestionIndex(prev => prev + 1);
    } else {
      submitTest();
    }
  };

  const submitTest = async () => {
    try {
      setSubmitting(true);
      const data = await placementService.submitAssessment(selectedRole, answers);
      setResult(data);
      setStage('result');
      toast.success('Assessment completed successfully!');
    } catch (error) {
      toast.error('Failed to submit assessment');
    } finally {
      setSubmitting(false);
    }
  };

  const resetTest = () => {
    setStage('role-selection');
    setSelectedRole(null);
    setResult(null);
  };

  // ---------------------------------------------------------
  // RENDER: Role Selection
  // ---------------------------------------------------------
  const renderRoleSelection = () => (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="text-center mb-10">
        <p className="text-4xl font-bold text-muted-foreground mt-2">
          SKILL ANALYZER
        </p>
        <p className="text-base text-muted-foreground mt-2">
          Select your target role to assess your skills and see your readiness score.
        </p>
      </div>

      {loading ? (
        <div className="flex justify-center items-center h-48">
          <div className="w-12 h-12 rounded-full border-4 border-primary/30 border-t-primary animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {roles.map((role) => (
            <motion.div
              key={role.name}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              whileHover={{ y: -5 }}
              transition={{ duration: 0.2 }}
            >
              <Card
                className="h-full cursor-pointer hover:border-primary/50 transition-colors bg-card/50 backdrop-blur-sm group overflow-hidden relative"
                onClick={() => startTest(role.name)}
              >
                <div className="absolute inset-0 bg-gradient-to-br from-primary/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
                <CardHeader>
                  <div className="w-12 h-12 rounded-lg bg-primary/10 flex items-center justify-center mb-4 text-primary">
                    <Briefcase className="w-6 h-6" />
                  </div>
                  <CardTitle>{role.name}</CardTitle>
                  <CardDescription>{role.description}</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="text-sm font-medium">Key Skills Assessed:</div>
                    <div className="flex flex-wrap gap-2">
                      {role.keySkills.map(skill => (
                        <span key={skill} className="px-2 py-1 bg-secondary/50 rounded-md text-xs">
                          {skill}
                        </span>
                      ))}
                    </div>
                  </div>
                  <div className="mt-6 flex items-center text-sm font-medium text-primary">
                    Start Assessment <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform" />
                  </div>
                </CardContent>
              </Card>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  );

  // ---------------------------------------------------------
  // RENDER: Test Taking
  // ---------------------------------------------------------
  const renderTestTaking = () => {
    if (!questions.length) return null;
    const currentQ = questions[currentQuestionIndex];
    const progress = ((currentQuestionIndex) / questions.length) * 100;

    return (
      <div className="max-w-3xl mx-auto space-y-8">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-xl font-semibold text-primary">{selectedRole} Assessment</h2>
          <span className="text-muted-foreground font-medium">
            Question {currentQuestionIndex + 1} of {questions.length}
          </span>
        </div>

        <Progress value={progress} className="h-2" />

        <AnimatePresence mode="wait">
          <motion.div
            key={currentQuestionIndex}
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: -20 }}
            className="mt-8"
          >
            <Card className="border-2 border-primary/20 shadow-lg bg-card/80 backdrop-blur">
              <CardHeader className="bg-primary/5 rounded-t-xl border-b border-primary/10 pb-6">
                <div className="flex items-center gap-2 mb-4">
                  <span className="px-3 py-1 bg-primary/20 text-primary rounded-full text-xs font-semibold uppercase tracking-wider">
                    {currentQ.category}
                  </span>
                  <span className="px-3 py-1 bg-secondary text-secondary-foreground rounded-full text-xs font-semibold">
                    {currentQ.difficulty}
                  </span>
                </div>
                <CardTitle className="text-2xl leading-relaxed">
                  {currentQ.questionText}
                </CardTitle>
              </CardHeader>
              <CardContent className="p-6">
                <div className="space-y-4 mt-2">
                  {currentQ.options.map((option, index) => {
                    const isSelected = answers[currentQuestionIndex]?.selectedAnswer === option;
                    return (
                      <motion.div
                        key={index}
                        whileHover={{ scale: 1.01 }}
                        whileTap={{ scale: 0.99 }}
                      >
                        <button
                          onClick={() => handleAnswerSelect(option)}
                          className={`w-full text-left p-4 rounded-xl border-2 transition-all flex items-center justify-between ${isSelected
                            ? 'border-primary bg-primary/10 shadow-sm'
                            : 'border-muted hover:border-primary/50 hover:bg-muted/50'
                            }`}
                        >
                          <span className={`font-medium ${isSelected ? 'text-primary' : 'text-foreground'}`}>
                            {option}
                          </span>
                          {isSelected && (
                            <div className="w-5 h-5 rounded-full bg-primary flex items-center justify-center">
                              <div className="w-2 h-2 rounded-full bg-primary-foreground" />
                            </div>
                          )}
                        </button>
                      </motion.div>
                    );
                  })}
                </div>
              </CardContent>
            </Card>

            <div className="flex justify-end mt-8">
              <Button
                size="lg"
                onClick={handleNext}
                disabled={submitting}
                className="w-full sm:w-auto px-8"
              >
                {submitting ? (
                  <>Processing <div className="ml-2 w-4 h-4 rounded-full border-2 border-current border-t-transparent animate-spin" /></>
                ) : currentQuestionIndex === questions.length - 1 ? (
                  <>Submit Assessment <CheckCircle2 className="w-5 h-5 ml-2" /></>
                ) : (
                  <>Next Question <ChevronRight className="w-5 h-5 ml-2" /></>
                )}
              </Button>
            </div>
          </motion.div>
        </AnimatePresence>
      </div>
    );
  };

  // ---------------------------------------------------------
  // RENDER: Result
  // ---------------------------------------------------------
  const renderResult = () => {
    if (!result) return null;

    return (
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="max-w-5xl mx-auto space-y-8"
      >
        {/* Score Header */}
        <div className="text-center space-y-4 mb-12 relative">
          <div className="absolute inset-0 -z-10 bg-gradient-to-b from-primary/10 to-transparent blur-3xl opacity-50" />
          <h1 className="text-3xl font-bold">Your Skill Analysis for</h1>
          <div className="text-4xl md:text-5xl font-extrabold text-primary">{result.targetRole}</div>

          <div className="mt-8 flex justify-center">
            <div className="relative w-48 h-48 flex items-center justify-center rounded-full border-8 border-primary/20 bg-background shadow-2xl">
              <svg className="absolute inset-0 w-full h-full -rotate-90">
                <circle
                  cx="92" cy="92" r="88"
                  className="stroke-primary"
                  strokeWidth="8" fill="none"
                  strokeDasharray="552.9"
                  strokeDashoffset={552.9 - (552.9 * result.readinessScore) / 100}
                  strokeLinecap="round"
                />
              </svg>
              <div className="text-center">
                <div className="text-5xl font-black text-foreground">{result.readinessScore}<span className="text-2xl text-muted-foreground">%</span></div>
                <div className="text-sm font-semibold text-primary uppercase tracking-widest mt-1">{result.readinessLabel}</div>
              </div>
            </div>
          </div>
        </div>

        <div className="space-y-8">
          {/* Skill Breakdown */}
          <div className="space-y-6">
            <h3 className="text-2xl font-bold flex items-center"><Target className="w-6 h-6 mr-2 text-primary" /> Skill Gap Analysis</h3>
            <div className="grid gap-4">
              {result.skillScores.map(skill => (
                <Card key={skill.skillName} className={`overflow-hidden ${skill.isMet ? 'border-l-4 border-l-green-500' : 'border-l-4 border-l-destructive'}`}>
                  <CardContent className="p-5 flex items-center justify-between gap-4">
                    <div className="flex-1">
                      <div className="flex justify-between mb-2">
                        <span className="font-semibold">{skill.skillName}</span>
                        <span className="font-mono">{skill.userScore}% / {skill.requiredLevel}% req</span>
                      </div>
                      <Progress
                        value={skill.userScore}
                        className="h-2"
                        indicatorClassName={skill.isMet ? 'bg-green-500' : 'bg-destructive'}
                      />
                    </div>
                    <div className="shrink-0 flex items-center">
                      {skill.isMet ? (
                        <div className="bg-green-500/10 text-green-500 p-2 rounded-full">
                          <Check className="w-5 h-5" />
                        </div>
                      ) : (
                        <div className="bg-destructive/10 text-destructive p-2 rounded-full">
                          <X className="w-5 h-5" />
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>

            <h3 className="text-2xl font-bold flex items-center mt-10 pt-4"><BrainCircuit className="w-6 h-6 mr-2 text-primary" /> ML Recommendations</h3>
            <div className="space-y-3">
              {result.recommendations.map((rec, i) => (
                <div key={i} className="flex items-start p-4 rounded-lg bg-card border border-border/50">
                  <div className="bg-primary/20 text-primary w-6 h-6 rounded-full flex items-center justify-center shrink-0 mt-0.5 mr-3 text-xs font-bold">{i + 1}</div>
                  <p className="text-sm leading-relaxed">{rec}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Removed Roadmap Sidebar */}
        </div>

        <div className="flex justify-center pt-8 border-t border-border mt-12">
          <Button onClick={resetTest} size="lg" variant="outline" className="mr-4">
            Take Another Assessment
          </Button>
          <Button onClick={() => window.print()} size="lg">
            Save Report
          </Button>
        </div>
      </motion.div>
    );
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-7xl">
      {stage === 'role-selection' && renderRoleSelection()}
      {stage === 'test-taking' && renderTestTaking()}
      {stage === 'result' && renderResult()}
    </div>
  );
};

export default PlacementReadiness;
